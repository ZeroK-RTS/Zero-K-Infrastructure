namespace ZkData
{
	partial class StructureType
	{

        public string GetImageUrl() {
            return string.Format((string)"/img/structures/{0}", (object)MapIcon);
        }

	    public override string ToString() {
	        return Name;
	    }
	}
}
