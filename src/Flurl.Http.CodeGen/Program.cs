﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flurl.Http.CodeGen
{
    class Program
    {
        static int Main(string[] args) {
	        var codePath = (args.Length > 0) ? args[0] : @"..\Flurl.Http\GeneratedExtensions.cs";

			if (!File.Exists(codePath)) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Code file not found: " + Path.GetFullPath(codePath));
				Console.ReadLine();
				return 2;
			}

			try {
				File.WriteAllText(codePath, "");
                using (var writer = new CodeWriter(codePath))
                {
                    writer
                        .WriteLine("// This file was auto-generated by Flurl.Http.CodeGen. Do not edit directly.")
	                    .WriteLine("using System;")
						.WriteLine("using System.Collections.Generic;")
                        .WriteLine("using System.IO;")
	                    .WriteLine("using System.Net;")
						.WriteLine("using System.Net.Http;")
                        .WriteLine("using System.Threading;")
                        .WriteLine("using System.Threading.Tasks;")
	                    .WriteLine("using Flurl.Http.Configuration;")
						.WriteLine("using Flurl.Http.Content;")
                        .WriteLine("")
                        .WriteLine("namespace Flurl.Http")
                        .WriteLine("{")
                        .WriteLine("/// <summary>")
                        .WriteLine("/// Auto-generated fluent extension methods on String, Url, and IFlurlRequest.")
                        .WriteLine("/// </summary>")
                        .WriteLine("public static class GeneratedExtensions")
                        .WriteLine("{");

                    WriteExtensionMethods(writer);

                    writer
                        .WriteLine("}")
                        .WriteLine("}");
                }

                Console.WriteLine("File writing succeeded.");
				return 0;
            }
            catch (Exception ex) {
	            Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
	            Console.ReadLine();
                return 2;
            }
        }

        private static void WriteExtensionMethods(CodeWriter writer)
        {
			string name = null;
            foreach (var xm in HttpExtensionMethod.GetAll()) {
	            var hasRequestBody = (xm.HttpVerb == "Post" || xm.HttpVerb == "Put" || xm.HttpVerb == "Patch" || xm.HttpVerb == null);

				if (xm.Name != name) {
		            Console.WriteLine($"writing {xm.Name}...");
		            name = xm.Name;
	            }
	            writer.WriteLine("/// <summary>");
                var summaryStart = (xm.ExtentionOfType == "IFlurlRequest") ? "Sends" : "Creates a FlurlRequest from the URL and sends";
				if (xm.HttpVerb == null)
					writer.WriteLine("/// @0 an asynchronous request.", summaryStart);
				else
					writer.WriteLine("/// @0 an asynchronous @1 request.", summaryStart, xm.HttpVerb.ToUpperInvariant());
                writer.WriteLine("/// </summary>");
				if (xm.ExtentionOfType == "IFlurlRequest")
                    writer.WriteLine("/// <param name=\"request\">The IFlurlRequest instance.</param>");
                if (xm.ExtentionOfType == "Url" || xm.ExtentionOfType == "string")
                    writer.WriteLine("/// <param name=\"url\">The URL.</param>");
				if (xm.HttpVerb == null)
					writer.WriteLine("/// <param name=\"verb\">The HTTP method used to make the request.</param>");
				if (xm.BodyType != null)
					writer.WriteLine("/// <param name=\"data\">Contents of the request body.</param>");
				else if (hasRequestBody)
					writer.WriteLine("/// <param name=\"content\">Contents of the request body.</param>");
				writer.WriteLine("/// <param name=\"cancellationToken\">A cancellation token that can be used by other objects or threads to receive notice of cancellation. Optional.</param>");
				writer.WriteLine("/// <param name=\"completionOption\">The HttpCompletionOption used in the request. Optional.</param>");
				writer.WriteLine("/// <returns>A Task whose result is @0.</returns>", xm.ReturnTypeDescription);

                var args = new List<string>();
                args.Add("this " + xm.ExtentionOfType + (xm.ExtentionOfType == "IFlurlRequest" ? " request" : " url"));
	            if (xm.HttpVerb == null)
		            args.Add("HttpMethod verb");
                if (xm.BodyType != null)
                    args.Add((xm.BodyType == "String" ? "string" : "object") + " data");
				else if (hasRequestBody)
					args.Add("HttpContent content");

				// http://stackoverflow.com/questions/22359706/default-parameter-for-cancellationtoken
				args.Add("CancellationToken cancellationToken = default(CancellationToken)");
	            args.Add("HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead");

				writer.WriteLine("public static Task<@0> @1@2(@3) {", xm.TaskArg, xm.Name, xm.IsGeneric ? "<T>" : "", string.Join(", ", args));

                if (xm.ExtentionOfType == "IFlurlRequest")
                {
	                args.Clear();
                    args.Add(
						xm.HttpVerb == null ? "verb" :
						xm.HttpVerb == "Patch" ? "new HttpMethod(\"PATCH\")" : // there's no HttpMethod.Patch
						"HttpMethod." + xm.HttpVerb);

                    if (xm.BodyType != null || hasRequestBody)
                        args.Add("content: content");

					args.Add("cancellationToken: cancellationToken");
					args.Add("completionOption: completionOption");

					if (xm.BodyType != null) {
		                writer.WriteLine("var content = new Captured@0Content(@1);",
			                xm.BodyType,
			                xm.BodyType == "String" ? "data" : $"request.Settings.{xm.BodyType}Serializer.Serialize(data)");
	                }

                    var request = (xm.ExtentionOfType == "IFlurlRequest") ? "request" : "new FlurlRequest(url)";
                    var receive = (xm.DeserializeToType == null) ? "" : string.Format(".Receive{0}{1}()", xm.DeserializeToType, xm.IsGeneric ? "<T>" : "");
                    writer.WriteLine("return @0.SendAsync(@1)@2;", request, string.Join(", ", args), receive);
                }
                else
                {
                    writer.WriteLine("return new FlurlRequest(url).@0(@1);",
                        xm.Name + (xm.IsGeneric ? "<T>" : ""),
                        string.Join(", ", args.Skip(1).Select(a => a.Split(' ')[1])));
                }

                writer.WriteLine("}").WriteLine();
            }

	        foreach (var xtype in new[] { "Url", "string" }) {
		        foreach (var xm in UrlExtensionMethod.GetAll()) {
			        if (xm.Name != name) {
				        Console.WriteLine($"writing {xm.Name}...");
				        name = xm.Name;
			        }

			        writer.WriteLine("/// <summary>");
			        writer.WriteLine($"/// {xm.Description}");
			        writer.WriteLine("/// </summary>");
			        writer.WriteLine("/// <param name=\"url\">The URL.</param>");
			        foreach (var p in xm.Params)
				        writer.WriteLine($"/// <param name=\"{p.Name}\">{p.Description}</param>");
			        writer.WriteLine("/// <returns>The IFlurlRequest.</returns>");

			        var argList = new List<string> { $"this {xtype} url" };
			        argList.AddRange(xm.Params.Select(p => $"{p.Type} {p.Name}" + (p.Default == null ? "" : $" = {p.Default}")));
			        writer.WriteLine($"public static IFlurlRequest {xm.Name}({string.Join(", ", argList)}) {{");
			        writer.WriteLine($"return new FlurlRequest(url).{xm.Name}({string.Join(", ", xm.Params.Select(p => p.Name))});");
			        writer.WriteLine("}");
		        }
	        }
        }
    }
}