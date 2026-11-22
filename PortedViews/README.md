# Views the .NET 9 port compiles instead of the originals

Every other Razor view is **linked** out of `Zero-K.info/Views` and compiled unmodified by both
stacks. The files here are the exception: the port compiles these copies, and
`Zero-K.info/Views/<same path>` stays exactly as it is for the MVC 5 build.

They exist for one reason. **ASP.NET Core removed child actions.** The replacement is a view
component, and its call site is

    @await Component.InvokeAsync("ForumPostList", new { threadID = ... })

which does not compile on MVC 5, where the same place reads

    @Html.Action("GetPostList", "Forum", new { threadID = ... })

There is no spelling that satisfies both, so this is the first and so far only place where one
source file cannot serve both builds.

## The cost, stated plainly

A view in here is maintained twice until `Zero-K.info/asp.net.csproj` is retired, and the MVC 5
copy is the one serving production. An edit to `Zero-K.info/Views/Shared/CommentList.cshtml`
that is not repeated here will not show up on .NET 9, and nothing will report it -
`tools/view-port-report.sh` compiles both copies and so catches a compile error, but it cannot
see a *behaviour* difference between two files that both compile.

Keep the diff to the child-action call sites and nothing else. A diverged view that has also
drifted for unrelated reasons is much harder to merge back.

## How it is wired

`port-views.props` is imported by `ZeroKWeb.Core`, `ZeroKWeb.Host` and `ZeroKWeb.Render`. It
removes the linked original and links the copy here in its place, at the same `Views\...` path,
so `~/Views/Shared/CommentList.cshtml` resolves to this file in the port and to the original in
the MVC 5 build.

Adding one means: copy the original here, change only the child-action call, add a
`<Content Remove>` line to `port-views.props`, write the view component, and re-record the
inventory.

## What is still blocked

A diverged view **compiles**; it does not follow that it renders. Every one of the seven actions
reached by a child action renders a partial that does not compile yet - `Forum/PostList`,
`Poll/PollView`, `Planetwars/Ladder`, `Planetwars/PwMatchMaker`, `Shared/ChatNotification`,
`Planetwars/Events`. Until those are ported, a view component can be written and compiled but
not exercised, which is why only one is here.
