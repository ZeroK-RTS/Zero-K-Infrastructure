// EF6 spelled this System.Data.Entity.EntityState and EF Core spells it
// Microsoft.EntityFrameworkCore.EntityState, with identical members. Entity code writes the
// bare name with `using System.Data.Entity`, and until now this project answered that with a
// shim enum of its own - which meant two structurally identical types, and an error wherever
// one met the other (ZkDataContext.EntityEntry.State against db.Entry().State, and any web
// code doing both).
//
// One alias removes the second type entirely. The shim enum is gone.
global using EntityState = Microsoft.EntityFrameworkCore.EntityState;
