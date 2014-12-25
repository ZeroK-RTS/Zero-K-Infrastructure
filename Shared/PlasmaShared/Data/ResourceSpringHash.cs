using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;

namespace ZkData
{
  partial class ResourceSpringHash
  {
      // HACK pending implementation
      /*
    partial void OnValidate(ChangeAction action)
    {
      if (action == ChangeAction.Insert || action == ChangeAction.Update)
      {
        if (Resource != null) Resource.LastChange = DateTime.UtcNow;
      }
    }*/
  }
}
