using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace System.Web
{
    /// <summary>
    /// MVC 5's uploaded file, over ASP.NET Core's.
    ///
    /// **This was deliberately left unshimmed for most of the port**, and the reason is recorded
    /// in ZkData/EFCORE-MIGRATION.md: a type alone compiles, and then ASP.NET Core's model binder
    /// has nothing that produces it, so every upload action receives null and silently does
    /// nothing. That is worse than not compiling - four upload paths would have looked ported and
    /// quietly dropped every file.
    ///
    /// What makes it safe now is not the wrapper. It is <see cref="HttpPostedFileBinderProvider"/>
    /// below, and the check in ZeroKWeb.Host that POSTs a real multipart form and asserts the
    /// bytes arrive. The wrapper without the binder is the trap; the binder without the check is
    /// a claim.
    ///
    /// The surface is only what the four call sites use, counted rather than guessed:
    /// ContentLength (NewsController, ClansController), InputStream (NewsController),
    /// FileName (NewsController, MapsController) and SaveAs (MapsController).
    /// </summary>
    public class HttpPostedFileBase
    {
        private readonly IFormFile file;

        public HttpPostedFileBase(IFormFile file) => this.file = file;

        /// <summary>
        /// MVC 5's int, over ASP.NET Core's long. Both call sites compare it to 0, so the
        /// narrowing cannot bite them - but it is a narrowing, and a 2GB upload would wrap.
        /// Clamped rather than cast for that reason.
        /// </summary>
        public int ContentLength => file.Length > int.MaxValue ? int.MaxValue : (int)file.Length;

        public Stream InputStream => file.OpenReadStream();

        /// <summary>
        /// The name the client sent. ASP.NET Core does not strip a path from it and neither did
        /// MVC 5, which is why MapsController combines it into a directory - that is a
        /// pre-existing path-traversal shape, not one introduced here, and it is left alone
        /// rather than silently changed during a port.
        /// </summary>
        public string FileName => file.FileName;

        public string ContentType => file.ContentType;

        public void SaveAs(string filename)
        {
            using (var stream = File.Create(filename)) file.CopyTo(stream);
        }
    }

    /// <summary>
    /// Binds <see cref="HttpPostedFileBase"/> from the posted form, which is the whole point.
    ///
    /// ASP.NET Core binds IFormFile out of the box and knows nothing about MVC 5's type; without
    /// this, an action taking one compiles and always receives null. Registered in
    /// ZeroKWeb.Host; any host that links these controllers has to register it too, and the
    /// upload check is what will say so if it does not.
    /// </summary>
    public class HttpPostedFileBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

            var name = bindingContext.FieldName;
            var files = bindingContext.HttpContext.Request.HasFormContentType
                ? bindingContext.HttpContext.Request.Form.Files
                : null;

            var file = files?.GetFile(name);

            // MVC 5 handed the action null for an empty or absent file rather than an object with
            // nothing in it, and both ClansController and NewsController test for null first.
            bindingContext.Result = file == null || file.Length == 0
                ? ModelBindingResult.Success(null)
                : ModelBindingResult.Success(new HttpPostedFileBase(file));

            return Task.CompletedTask;
        }
    }

    public class HttpPostedFileBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.Metadata.ModelType == typeof(HttpPostedFileBase)
                ? new HttpPostedFileBinder()
                : null;
        }
    }
}
