namespace PlasmaShared
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