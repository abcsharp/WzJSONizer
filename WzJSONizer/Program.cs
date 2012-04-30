using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MapleLib.WzLib;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzStructure;
using MapleLib.WzLib.WzStructure.Data;
using MapleLib.WzLib.WzProperties;
using MapleLib.WzLib.Serialization;
using Json;

namespace WzJSONizer
{
	using DirIter=IEnumerator<WzDirectory>;
	using PropIter=IEnumerator<IWzImageProperty>;
	
	class DirObject
	{
		public DirIter Dir;
		public Dictionary<string,object> Hash;

		public DirObject(DirIter Dir,Dictionary<string,object> Hash)
		{
			this.Dir=Dir;
			this.Hash=Hash;
		}
	};

	class PropObject
	{
		public PropIter Prop;
		public Dictionary<string,object> Hash;

		public PropObject(PropIter Prop,Dictionary<string,object> Hash)
		{
			this.Prop=Prop;
			this.Hash=Hash;
		}
	};

	class Program
	{
		static void ParseFile(string InputFileName,string OutputFileName)
		{
			JsonCreator Creator=new JsonCreator();
			WzFile File=new WzFile(InputFileName,WzMapleVersion.BMS);
			File.ParseWzFile();
			List<WzDirectory> RootDirectory=new List<WzDirectory>();
			RootDirectory.Add(File.WzDirectory);
			var Dirs=new Stack<DirObject>();
			var RootHash=new Dictionary<string,object>();
			Dirs.Push(new DirObject(RootDirectory.GetEnumerator(),RootHash));
			for(;Dirs.Count>0;){
				if(!Dirs.Peek().Dir.MoveNext()){
					Dirs.Pop();
					continue;
				}else{
					foreach(WzImage Image in Dirs.Peek().Dir.Current.WzImages){
						var Props=new Stack<PropObject>();
						var Temp1=new Dictionary<string,object>();
						Dirs.Peek().Hash.Add(Image.Name,Temp1);
						Props.Push(new PropObject(Image.WzProperties.GetEnumerator(),Temp1));
						for(;Props.Count>0;){
							if(!Props.Peek().Prop.MoveNext()){
								Props.Pop();
								continue;
							}
							var Obj=Props.Peek().Prop.Current;
							switch(Props.Peek().Prop.Current.PropertyType){
							case WzPropertyType.ByteFloat:
								Props.Peek().Hash.Add(Obj.Name,(double)((WzByteFloatProperty)Obj).Value);
								break;
							case WzPropertyType.Canvas:
								//Not Implemented
								break;
							case WzPropertyType.CompressedInt:
								Props.Peek().Hash.Add(Obj.Name,(long)((WzCompressedIntProperty)Obj).Value);
								break;
							case WzPropertyType.Convex:
								//Not Implemented
								break;
							case WzPropertyType.Double:
								Props.Peek().Hash.Add(Obj.Name,((WzDoubleProperty)Obj).Value);
								break;
							case WzPropertyType.Null:
								Props.Peek().Hash.Add(Obj.Name,null);
								break;
							case WzPropertyType.PNG:
								var Temp2=((WzPngProperty)Obj).GetCompressedBytes(false).OfType<long>().ToList();
								var Temp3=new Dictionary<string,object>();
								Temp3.Add("Width",(long)((WzPngProperty)Obj).Width);
								Temp3.Add("Height",(long)((WzPngProperty)Obj).Height);
								Temp3.Add("Data",Temp2);
								break;
							case WzPropertyType.Sound:
								var Temp4=((WzSoundProperty)Obj).GetBytes(false).OfType<long>().ToList();
								var Temp5=new Dictionary<string,object>();
								Temp5.Add("Frequency",(long)((WzSoundProperty)Obj).Frequency);
								Temp5.Add("Length",(long)((WzSoundProperty)Obj).Length);
								Temp5.Add("Data",Temp4);
								Props.Peek().Hash.Add(Obj.Name,Temp5);
								break;
							case WzPropertyType.String:
								Props.Peek().Hash.Add(Obj.Name,((WzStringProperty)Obj).Value);
								break;
							case WzPropertyType.SubProperty:
								var Temp6=new Dictionary<string,object>();
								Props.Peek().Hash.Add(Obj.Name,Temp6);
								Props.Push(new PropObject(((WzSubProperty)Obj).WzProperties.GetEnumerator(),Temp6));
								break;
							case WzPropertyType.UnsignedShort:
								Props.Peek().Hash.Add(Obj.Name,(long)((WzUnsignedShortProperty)Obj).Value);
								break;
							case WzPropertyType.UOL:
								//Not Implemented
								break;
							case WzPropertyType.Vector:
								var Temp7=new Dictionary<string,object>();
								Temp7.Add("X",(long)((WzVectorProperty)Obj).X);
								Temp7.Add("Y",(long)((WzVectorProperty)Obj).Y);
								Props.Peek().Hash.Add(Obj.Name,Temp7);
								break;
							}
						}
					}
					var Temp8=new Dictionary<string,object>();
					Dirs.Peek().Hash.Add(Dirs.Peek().Dir.Current.Name,Temp8);
					Dirs.Push(new DirObject(Dirs.Peek().Dir.Current.WzDirectories.GetEnumerator(),Temp8));
				}
			}
			System.IO.File.WriteAllText(OutputFileName,Creator.Create(RootHash),Encoding.UTF8);
			return;
		}

		static void Main(string[] args)
		{
			string FileName;
			if(args.Length<2){
				Console.WriteLine("パース対象のwzファイルを指定して下さい。");
				FileName=Console.ReadLine();
			}else FileName=args[1];
			if(!File.Exists(FileName)){
				Console.WriteLine("指定されたファイルは存在しません。");
				return;
			}
			if(!Regex.IsMatch(FileName,".+\\.wz",RegexOptions.IgnoreCase)){
				Console.WriteLine("指定されたファイルはwzファイルではありません。");
				return;
			}
			ParseFile(FileName,"jsonresult.txt");
		}
	}
}
