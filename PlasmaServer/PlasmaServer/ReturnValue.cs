using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PlasmaServer
{
    public enum ReturnValue
    {
        Ok,
        InvalidLogin,
        ResourceNotFound,
        InternalNameAlreadyExistsWithDifferentSpringHash,
        Md5AlreadyExists,
        Md5AlreadyExistsWithDifferentName,
    }
}
