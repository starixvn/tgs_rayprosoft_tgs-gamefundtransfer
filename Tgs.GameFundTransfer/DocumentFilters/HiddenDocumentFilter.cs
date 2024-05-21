using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tgs.GameFundTransfer.Attributes;

namespace Tgs.GameFundTransfer.DocumentFilters;

public class HiddenDocumentFilter : IDocumentFilter
{
	public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
	{
		foreach (var apiDescription in context.ApiDescriptions)
		{
			if (apiDescription.TryGetMethodInfo(out var method))
			{
				if (method.ReflectedType!.CustomAttributes.Any(t => t.AttributeType == typeof(HiddenApiAttribute))
						|| method.CustomAttributes.Any(t => t.AttributeType == typeof(HiddenApiAttribute)))
				{
					var key = "/" + apiDescription.RelativePath;
					if (key.Contains('?'))
					{
						var idx = key.IndexOf('?');
						key = key[..idx];
					}

					_ = swaggerDoc.Paths.Remove(key);
				}
			}
		}
	}
}
