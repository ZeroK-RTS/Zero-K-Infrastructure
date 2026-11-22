// Builds the port's script and style bundles.
//
//     npm install && npm run build          -> build/assets/bundles/*
//
// **Why this exists.** The live site bundles with System.Web.Optimization, which is .NET
// Framework only and has no ASP.NET Core successor. Until now the port's BundlingCompat emitted
// every file as its own unminified tag - 11 script tags where the site serves one - and its own
// comment said "whoever picks the build step replaces this". This is that build step.
//
// It does NOT touch the live site. Changing the asset pipeline of the site that is serving
// players is a separate decision from giving the port one.
//
// The file list comes from tools/check-bundles.py --print, which reads BundlingCompat.cs and is
// itself checked against BundleConfig.cs. One parser, one list, and the thing that builds the
// bundle reads the same list the check compares.
import { execFileSync } from "node:child_process";
import { transform } from "esbuild";
import { mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), "..");
const site = path.join(root, "Zero-K.info");
const out = path.join(root, "build", "assets", "bundles");

const bundles = JSON.parse(
    execFileSync("python3", [path.join(root, "tools", "check-bundles.py"), "--print"], { encoding: "utf8" })
);

// "~/Scripts/jquery-{version}.js" is the placeholder the real bundler expands. Resolve it the
// way it does - the newest matching file - rather than hard-coding a version that will rot.
function resolve(tildePath) {
    const relative = tildePath.replace(/^~\//, "");
    if (!relative.includes("{version}")) return path.join(site, relative);

    const dir = path.join(site, path.dirname(relative));
    const pattern = path.basename(relative).replace("{version}", "");
    const [prefix, suffix] = pattern.split(/(?=\.js$)/);
    const matches = readdirSync(dir)
        .filter((f) => f.startsWith(prefix) && f.endsWith(suffix))
        // The bundler picks the plain file, not .min or the IDE's .intellisense helper.
        .filter((f) => !/\.min\.|\.intellisense\./.test(f))
        .sort();
    if (matches.length === 0) throw new Error("nothing matches " + tildePath);
    return path.join(dir, matches[matches.length - 1]);
}

rmSync(out, { recursive: true, force: true });
mkdirSync(out, { recursive: true });

for (const [name, files] of Object.entries(bundles)) {
    const resolved = files.map(resolve);
    const isCss = name.endsWith("css");
    const target = path.join(out, path.basename(name) + (isCss ? ".css" : ".js"));

    // CONCATENATED and minified, which is what System.Web.Optimization does - not bundled as a
    // module graph. These are 2010s browser scripts that talk to each other through globals, and
    // several carry UMD wrappers: the first version of this used esbuild's bundler and failed on
    // `require("jquery")` inside jquery.datetimepicker.full.min.js, which in a browser never runs
    // because the UMD falls through to the global branch.
    //
    // Joined with ";\n" for the same reason the real bundler does: a file that ends without a
    // semicolon followed by one starting with "(" is a different program than the two apart.
    //
    // Every url() in the stylesheets is absolute or a data: URI - checked - so concatenation
    // needs no path rewriting and the result can be served from anywhere.
    const source = resolved.map((f) => readFileSync(f, "utf8")).join(isCss ? "\n" : ";\n");

    const minified = await transform(source, {
        loader: isCss ? "css" : "js",
        minify: true,
        legalComments: "none",
    });

    writeFileSync(target, minified.code);
    console.log("  %s  <- %d files", path.relative(root, target), files.length);
}
