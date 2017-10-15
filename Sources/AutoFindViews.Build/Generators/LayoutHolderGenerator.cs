﻿namespace AutoFindViews.Build
{
	using System.Linq;
	using System.Xml.Linq;
	using System.IO;
	using System.Text;
	using Microsoft.Build.Evaluation;

	public class LayoutHolderGenerator
	{
		public LayoutHolderGenerator(Project project, ITypeMapper mapper, string nspace)
		{
			this.project = project;
			this.mapper = mapper;
			this.nspace = nspace;
		}

		public static string CreateClassName(string name)
		{
			var spits = name.Split('_');
			name = string.Join("",spits.Select(x => x.FirstLetterToUpper()));
			return $"{name}LayoutHolder";
		}

		private Project project;

		private ITypeMapper mapper;

		private string nspace;

		/// <summary>
		/// Generates the layout holder class from the axml file.
		/// </summary>
		/// <param name="xml">Axml content.</param>
		public bool Generate(string inputPath, string outputPath)
		{
			var xml = XElement.Load(inputPath);

			const string template = "LayoutHolder.Designer.cs";

			var name = Path.GetFileNameWithoutExtension(inputPath);
			var classname = CreateClassName(name);

			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}

			var content = Templates.LoadTemplate(template);

			var declarations = ViewPropertyDeclaration.ParseDeclarations(xml, mapper);

			var fields = new StringBuilder();
			var properties = new StringBuilder();
			foreach (var declaration in declarations)
			{
				fields.AppendLine($"\t\t// L{declaration.Line}: {declaration.Source}");
				fields.AppendLine($"\t\tprivate {declaration.Type} _{declaration.Id};\n");
				properties.AppendLine($"\t\t// L{declaration.Line}: {declaration.Source}");
				properties.Append($"\t\tpublic {declaration.Type} {declaration.Id} => _{declaration.Id} ?? (_{declaration.Id} = ");

				if(declaration.IsInclude)
				{
					properties.AppendLine($"new {declaration.Type}(Source.FindViewById<Android.Views.View>(Resource.Id.{declaration.Id})));\n");
				}
				else
				{
					properties.AppendLine($"Source.FindViewById<{declaration.Type}>(Resource.Id.{declaration.Id}));\n");
				}

			}

			content = string.Format(content, nspace, name, classname, fields, properties);
			File.WriteAllText(outputPath, content);

			return project.AddItemIfNotExists("Compile", outputPath);
		}
	}
}
