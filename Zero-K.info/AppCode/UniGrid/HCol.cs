namespace ZeroKWeb
{
    public class HCol
    {
        public HCol(string column, string description, bool orderby, string columncontent/*, int id*/)
        {
            Column = column;
            Description = description;
            ColumnContent = columncontent;
            OrderBy = orderby;
            //Id = id;
        }

        public HCol(string column, string description, bool orderby, object columncontent) 
            : this(column,description,orderby, columncontent.ToString()) {}        

        public string ColumnContent { get; private set; }

        public string Description { get; private set; }

        public string Column { get; private set; }

        public bool OrderBy { get; private set; }

        //public int Id { get; set; }
    }
}