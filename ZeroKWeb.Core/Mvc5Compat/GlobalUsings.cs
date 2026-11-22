// MVC 5's HtmlHelper is a class; ASP.NET Core's IHtmlHelper is an interface, and everything
// the ForumParser does with one - pass it along, call extension methods on it - works
// identically against the other.
//
// A global using alias maps the name, so 40 files and 2000 lines of parser link without an
// edit. The alternative was changing the parameter type in production code, which would
// break the MVC 5 build that still runs the site.
global using HtmlHelper = Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper;

// ActionLink, Partial, Raw and the rest of ASP.NET Core's helper surface are extension
// methods on IHtmlHelper, not members of it. The alias above maps the type name; without the
// namespace in scope the linked files see only the interface's own members and fail with
// "no argument given for 'fragment'" on what looks like a perfectly ordinary ActionLink.
global using Microsoft.AspNetCore.Mvc.Rendering;

// MVC 5's Controller, ActionResult, ViewResult, [HttpPost] and the rest live in
// System.Web.Mvc; ASP.NET Core's carry the same names in Microsoft.AspNetCore.Mvc. Bringing
// that namespace in globally lets a linked controller find them, and the shim namespace
// System.Web.Mvc deliberately defines none of those names, so nothing is ambiguous.
global using Microsoft.AspNetCore.Mvc;

// Controllers write `db.Entry(x).State = EntityState.Added`. EF Core's Entry().State wants
// EF Core's enum; `using System.Data.Entity` finds ZkData.Core's shim enum instead, and two
// structurally identical enums are still two types (CS0266).
//
// Scoped here rather than fixed in ZkData.Core: that shim exists because ENTITY code
// compares `entry.State == EntityState.Modified` against ZkDataContext.EntityEntry, whose
// State really is the EF6-shaped type. Both readings are right in their own project, and a
// global alias in this one settles it for linked controllers without touching that.
global using EntityState = Microsoft.EntityFrameworkCore.EntityState;

// The Request/Response/page shims live in ZeroKWeb.Compat, and linked controllers have no
// using for it - they were written when these members were on the framework's own types.
global using ZeroKWeb.Compat;

// MVC 5's ActionFilterAttribute, ActionExecutingContext and friends sit in System.Web.Mvc;
// ASP.NET Core keeps the same names in Microsoft.AspNetCore.Mvc.Filters. MapsController
// declares a filter, and AuthAttribute implements one.
global using Microsoft.AspNetCore.Mvc.Filters;

// System.Web.WebPages.HelperResult, which AppCode/UniGrid/Col.cs names in a Func<object,
// HelperResult>. ASP.NET Core's lives in Microsoft.AspNetCore.Mvc.Razor and is what a templated
// Razor delegate actually produces, so the alias points there rather than at a shim - a shim
// subclass would be a different type from what the views hand it.
global using HelperResult = Microsoft.AspNetCore.Mvc.Razor.HelperResult;
