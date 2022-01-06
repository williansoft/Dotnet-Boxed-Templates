namespace ApiTemplate.ConfigureOptions;

using ApiTemplate.Options;
using Boxed.AspNetCore;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

public class ConfigureMvcOptions : IConfigureOptions<MvcOptions>
{
    private readonly CacheProfileOptions cacheProfileOptions;

    public ConfigureMvcOptions(CacheProfileOptions cacheProfileOptions) =>
        this.cacheProfileOptions = cacheProfileOptions;

    public void Configure(MvcOptions options)
    {
        // Controls how controller actions cache content from the appsettings.json file.
        if (this.cacheProfileOptions is not null)
        {
            foreach (var keyValuePair in this.cacheProfileOptions)
            {
                options.CacheProfiles.Add(keyValuePair);
            }
        }

        // Remove plain text (text/plain) output formatter.
        options.OutputFormatters.RemoveType<StringOutputFormatter>();

        // Add support for JSON Patch (application/json-patch+json) by adding an input formatter.
        options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());

        var jsonInputFormatterMediaTypes = options
            .InputFormatters
            .OfType<SystemTextJsonInputFormatter>()
            .First()
            .SupportedMediaTypes;
        var jsonOutputFormatterMediaTypes = options
            .OutputFormatters
            .OfType<SystemTextJsonOutputFormatter>()
            .First()
            .SupportedMediaTypes;

#if AnyXmlSerializer
        var xmlInputFormatterMediaTypes = options
            .InputFormatters
#if DataContractSerializer
            .OfType<XmlDataContractSerializerInputFormatter>()
#elif XmlSerializer
            .OfType<XmlSerializerInputFormatter>()
#endif
            .First()
            .SupportedMediaTypes;
        var xmlOutputFormatterMediaTypes = options
            .OutputFormatters
#if DataContractSerializer
            .OfType<XmlDataContractSerializerOutputFormatter>()
#elif XmlSerializer
            .OfType<XmlSerializerOutputFormatter>()
#endif
            .First()
            .SupportedMediaTypes;

        // Remove XML text (text/xml) media type from the XML input and output formatters.
        xmlInputFormatterMediaTypes.Remove("text/xml");
        xmlOutputFormatterMediaTypes.Remove("text/xml");

#endif
        // Remove JSON text (text/json) media type from the JSON input and output formatters.
        jsonInputFormatterMediaTypes.Remove("text/json");
        jsonOutputFormatterMediaTypes.Remove("text/json");

        // Add ProblemDetails media type (application/problem+json) to the output formatters.
        // See https://tools.ietf.org/html/rfc7807
        jsonOutputFormatterMediaTypes.Insert(0, ContentType.ProblemJson);
#if AnyXmlSerializer
        xmlOutputFormatterMediaTypes.Insert(1, ContentType.ProblemXml);
#endif

        // Add RESTful JSON media type (application/vnd.restful+json) to the JSON input and output formatters.
        // See http://restfuljson.org/
        jsonInputFormatterMediaTypes.Insert(0, ContentType.RestfulJson);
        jsonOutputFormatterMediaTypes.Insert(0, ContentType.RestfulJson);

        // Returns a 406 Not Acceptable if the MIME type in the Accept HTTP header is not valid.
        options.ReturnHttpNotAcceptable = true;
    }

    /// <summary>
    /// Gets the JSON patch input formatter. The <see cref="JsonPatchDocument{T}"/> does not support the new
    /// System.Text.Json API's for de-serialization. You must use Newtonsoft.Json instead (See
    /// https://docs.microsoft.com/en-us/aspnet/core/web-api/jsonpatch?view=aspnetcore-3.0#jsonpatch-addnewtonsoftjson-and-systemtextjson).
    /// </summary>
    /// <returns>The JSON patch input formatter using Newtonsoft.Json.</returns>
    private static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddMvcCore()
            .AddNewtonsoftJson()
            .Services;
        var serviceProvider = services.BuildServiceProvider();
        var mvcOptions = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Value;
        return mvcOptions.InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>()
            .First();
    }
}
