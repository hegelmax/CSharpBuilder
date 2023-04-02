using System;
//using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace LEMEX {
	public class Start {
		public static void Main() {
			CSharpBuilder CSharpBuilderInstance = new CSharpBuilder();
			CSharpBuilderInstance.Run();
		}
	}
	
	public class CSharpBuilder {
		// Specify the paths
		string _csc_file_path			= @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe";
		static string _project_path		= Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		string _cur_dir_name			= Path.GetFileName(_project_path);
		string _config_file_path		= _project_path+"\\app.config";
		string _project_dir				= "";
		string _icon_file_path			= "";
		int _config_is_example			= 1;
		
		string _company_name			= "";
		string _project_name			= "";
		string _build_configuration		= "";
		string _build_target			= "";
		string[] _resources				= {};
		string[] _references			= {};
		string[] _sources				= {};
		
		public void Run() {
			if (!File.Exists(_config_file_path)) {
				File.WriteAllText(_config_file_path, getConfigExample());
				Console.WriteLine("Config file is missing!");
				return;
			}
			
			try {
				getConfig();
				
				if(_config_is_example != 0) {
					Console.WriteLine("Config file is in example mode!");
					return;
				}
				
				if (!Directory.Exists(this._project_dir)) {
					Directory.CreateDirectory(this._project_dir);
				}
				
				string[] files = Directory.GetFiles(_project_path, "*.cs", SearchOption.AllDirectories);
				foreach (string file in files) {
					string file_name	= Path.GetFullPath(file);
					Array.Resize(ref this._sources, this._sources.Length + 1);
					this._sources[this._sources.Length - 1] = file_name;
				}
				
				string debug = (this._build_configuration == "Release" ? "/debug-" : "/debug+");
				
				string[] options = new string[] {
					//"/noconfig",
					//"/nowarn:1701`,1702",
					//"/nostdlib-",
					//"/errorreport:prompt",
					//"/warn:4",
					debug,
					//$"/define:{build_configuration}``;TRACE",
					//"/optimize+",
					(this._build_target == "dll" ? "" : (string.IsNullOrEmpty(this._icon_file_path) ? "" : "/win32icon:"+this._icon_file_path)),
					"/out:\""+this._project_dir+"\\"+(string.IsNullOrEmpty(this._company_name) ? "" : this._company_name + "_")+ this._project_name + "." + (this._build_target == "dll" ? "dll" : "exe")+"\"",
					(this._build_target == "dll" ? "/target:library" : "/target:"+this._build_target)
				};
				
				string cmd = BuildCscCommand(options, this._sources, this._references, this._resources);
				
				ExecuteCommandString(cmd ,this._csc_file_path);
			}
			catch (Exception e){
				Console.WriteLine("ERROR: "+e.Message);
			}
		}
		
		void getConfig() {
			XmlDocument xmlDoc			= new XmlDocument();
			xmlDoc.Load(_config_file_path);
			XmlNode root				= xmlDoc.DocumentElement;
			XmlNode	node;
			
			node = root.SelectSingleNode("is_example");
			int.TryParse(((node == null) ? "0" : node.InnerText), out this._config_is_example);
			
			node = root.SelectSingleNode("company_name");
			this._company_name			= (node == null) ? "" : node.InnerText;
			
			node = root.SelectSingleNode("project_name");
			this._project_name			= (node == null) ? "" : (string.IsNullOrEmpty(node.InnerText) ? this._cur_dir_name : node.InnerText);
			
			node = root.SelectSingleNode("build_configuration");
			if (node == null) throw new Exception("Build Configuration is not specified");
			this._build_configuration	= node.InnerText;
			
			node = root.SelectSingleNode("build_target");
			if (node == null) throw new Exception("Build Target is not specified");
			this._build_target			= node.InnerText;
			
			this._project_dir			= _project_path+@"\bin\"+_build_configuration;
			
			// Set Icon
			string[] iconFiles			= Directory.GetFiles(_project_path, "*.ico");
			string firstIconFile		= iconFiles.FirstOrDefault();
			this._icon_file_path		= root.SelectSingleNode("icon_file_path").InnerText;
			this._icon_file_path		= (string.IsNullOrEmpty(this._icon_file_path) ? firstIconFile : this._icon_file_path);
			
			XmlNodeList resources_nodes		= root.SelectNodes("//resource_files/*");
			foreach (XmlNode n in resources_nodes) {
				Array.Resize(ref this._resources, this._resources.Length + 1);
				this._resources[this._resources.Length - 1] = n.InnerText;
			}
			
			XmlNodeList references_nodes	= root.SelectNodes("//reference_files/*");
			foreach (XmlNode n in references_nodes) {
				Array.Resize(ref this._references, this._references.Length + 1);
				this._references[this._references.Length - 1] = n.InnerText;
			}
			
			XmlNodeList sources_nodes	= root.SelectNodes("//source_files/*");
			foreach (XmlNode n in sources_nodes) {
				Array.Resize(ref this._sources, this._sources.Length + 1);
				this._sources[this._sources.Length - 1] = n.InnerText;
			}
		}
		
		static string BuildCscCommand(string[] options, string[] source_files, string[] reference_files, string[] resource_files) {
			string delim	= "\"";
			string opts		= string.Join(" ", options);
			
			if (reference_files.Length > 0) {
				string referencesString = string.Join(delim+" /reference:"+delim, reference_files);
				opts += " /reference:"+delim+referencesString+delim;
			}
			
			if (resource_files.Length > 0) {
				string resourcesString = string.Join(delim+" /resource:"+delim, resource_files);
				opts += " /resource:"+delim+resourcesString+delim;
			}
			
			if (source_files.Length > 0) {
				string sourcesString = string.Join(delim+" "+delim, source_files);
				opts += " "+delim+sourcesString+delim;
			}
			
			return opts;
		}
		
		static void ExecuteCommandString(string cmd, string csc_file_path) {
			// this drove me crazy... all I wanted to do was execute
			// something like this (excluding the [])
			//
			// [& $csc $opts] OR [& $cmd]
			//
			// however couldn't figure out the correct powershell syntax...
			// But I was able to execute it if I wrote the string out to a 
			// file and executed it from there... would be nice to not 
			// have to do that.
			
			Console.WriteLine();
			Console.WriteLine("*********** Executing Command ***********");
			//Console.WriteLine("Set-Location (Split-Path $MyInvocation.MyCommand.Path)");
			Console.WriteLine(cmd);
			Console.WriteLine("*****************************************");
			Console.WriteLine();
			Console.WriteLine();
			
			//string scriptFile	= CreateTempFile(cmd, csc_file_path);
			ExecuteCMD(cmd, csc_file_path);
			//ExecutePowerShellFile(cmd, this.csc_file_path);
		}
		
		static string CreateTempFile(string command, string csc_file_path){
			string tempFileGuid	= Guid.NewGuid().ToString();
			string scriptFile	= ".\\temp_build_csc_command-"+tempFileGuid+".ps1";
			RemoveIfExist(scriptFile);
			
			using (StreamWriter sw = File.AppendText(scriptFile)) {
				string cmd = csc_file_path+" "+command;
				sw.WriteLine(cmd);
			}
			return scriptFile;
		}
		
		static void ExecutePowerShellFile(string command, string csc_file_path) {
			string scriptFile	= CreateTempFile(command, csc_file_path);
			
			ProcessStartInfo psi = new ProcessStartInfo(scriptFile) {
				UseShellExecute = false,
				CreateNoWindow = true
			};
			Process.Start(psi).WaitForExit();
			
			RemoveIfExist(scriptFile);
		}
		
		static void ExecuteCMD(string command, string csc_file_path) {
			Process process = new Process();
			process.StartInfo.FileName					= csc_file_path;
			process.StartInfo.Arguments					= command;
			process.StartInfo.UseShellExecute			= false;
			process.StartInfo.RedirectStandardOutput	= true;
			process.Start();

			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			
			Console.WriteLine(output);
		}
		
		static void RemoveIfExist(string filePath) {
			if (File.Exists(filePath)) {
				File.Delete(filePath);
			}
		}
		
		static string getConfigExample() {
			return
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
	<!-- Creating compiled C# file -->
	<!-- To use this file filled with your information -->
	<!-- Then delete this node or set value to 0 -->
	<is_example>1</is_example>
	
	<!-- Set your Company name -->
	<!-- Using in final binary file name -->
	<!-- This value could be empty -->
	<company_name>YourCompanyName</company_name>
	
	<!-- Set your Program name -->
	<!-- This value could be empty -->
	<!-- If empty then ProgramName = Project Folder Name -->
	<project_name>YourProgramName</project_name>
	
	<build_configuration>Debug</build_configuration>
	<!--build_configuration>Release</build_configuration-->
	
	<build_target>exe</build_target>
	<!--build_target>winexe</build_target-->
	<!--build_target>dll</build_target-->
	
	<!-- This value could be empty -->
	<icon_file_path></icon_file_path>
	
	<resource_files>
	</resource_files>
	
	<reference_files>
		<!-- Example
		<item>C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.5\System.Core.dll</item>
		<item>C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll</item>
		-->
	</reference_files>
	
	<source_files>
		<!-- Example
		<item>D:\MyProjects\OtherProject\bin\Release\OtherProject.dll</item>
		-->
	</source_files>
</configuration>";
		}
	}
}