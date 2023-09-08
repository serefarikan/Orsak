using Microsoft.AspNetCore.Routing;
using Orsak;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ClassLibrary1
{
    public static class Class1
    {
        public static IEndpointConventionBuilder MapGetx<T, R, E>(this IEndpointRouteBuilder builder, string route, Func<T, Effect<R, IResult, E>> effect)
        {
            return builder.MapGet(route, async ([FromServices] R r, [FromRoute]T x) =>
             {
                 var result = await effect.Invoke(x).Run(r);
                 if (result.IsOk)
                 {
                     var a = result.ResultValue;
                     return a;
                 }
                 else
                 {
                     var err = result.ErrorValue;
                     return Results.BadRequest(err);
                 }
             });
        }
    }
}