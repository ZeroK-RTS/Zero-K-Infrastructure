namespace ZeroKLobby
{ 
    //Copied this from: http://blogs.artinsoft.net/Mrojas/archive/2009/08/07/Extended-WebBrowser-Control-Series-NewWindow3.aspx
    //"BrowserTab.cs" will use this to get NewWindow2 event (which return usefull information)
    //This require at least Window XP Service Pack 2. Reference: http://msdn.microsoft.com/en-us/library/aa768337(VS.85).aspx
    using System;
    using System.Windows.Forms;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    //First define a new EventArgs class to contain the newly exposed data
    public class NewWindow2EventArgs : CancelEventArgs
    {

        object ppDisp;

        public object PPDisp
        {
            get { return ppDisp; }
            set { ppDisp = value; }
        }


        public NewWindow2EventArgs(ref object ppDisp, ref bool cancel)
            : base()
        {
            this.ppDisp = ppDisp;
            this.Cancel = cancel;
        }
    }
    public class NewWindow3EventArgs : NewWindow2EventArgs
    {

        private uint _DwFlags;
        public uint Flags
        {
            get
            {
                return _DwFlags;
            }
            set
            {
                _DwFlags = value;
            }
        }
        private string _BstrUrlContext;
        public string UrlContext
        {
            get
            {
                return _BstrUrlContext;
            }
            set
            {
                _BstrUrlContext = value;
            }
        }
        private string _BstrUrl;
        public string Url
        {
            get
            {
                return _BstrUrl;
            }
            set
            {
                _BstrUrl = value;
            }
        }
        public NewWindow3EventArgs(ref object ppDisp, ref bool Cancel, uint dwFlags, string bstrUrlContext, string bstrUrl)
            : base(ref ppDisp, ref Cancel)
        {
            this.Flags = dwFlags;
            this.UrlContext = bstrUrlContext;
            this.Url = bstrUrl;
        }

    }





    public class DocumentCompleteEventArgs : EventArgs
    {
        private object ppDisp;
        private object url;

        public object PPDisp
        {
            get { return ppDisp; }
            set { ppDisp = value; }
        }

        public object Url
        {
            get { return url; }
            set { url = value; }
        }

        public DocumentCompleteEventArgs(object ppDisp, object url)
        {
            this.ppDisp = ppDisp;
            this.url = url;

        }
    }

    public class CommandStateChangeEventArgs : EventArgs
    {
        private long command;
        private bool enable;
        public CommandStateChangeEventArgs(long command, ref bool enable)
        {
            this.command = command;
            this.enable = enable;
        }

        public long Command
        {
            get { return command; }
            set { command = value; }
        }

        public bool Enable
        {
            get { return enable; }
            set { enable = value; }
        }
    }

    [Guid("332C4425-26CB-11D0-B483-00C04FD90119"), ComVisible(true), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IHTMLDocument2
    {
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object GetScript();
        /*later
        IHTMLElementCollection GetAll();
        IHTMLElement GetBody();
        IHTMLElement GetActiveElement();
        IHTMLElementCollection GetImages();
        IHTMLElementCollection GetApplets();
        IHTMLElementCollection GetLinks();
        IHTMLElementCollection GetForms();
        IHTMLElementCollection GetAnchors();*/
        void SetTitle(string p);
        string GetTitle();
        /*later IHTMLElementCollection GetScripts();*/
        void SetDesignMode(string p);
        string GetDesignMode();
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetSelection();
        string GetReadyState();
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetFrames();
        /*later
        IHTMLElementCollection GetEmbeds();
        IHTMLElementCollection GetPlugins(); */
        void SetAlinkColor(object c);
        object GetAlinkColor();
        void SetBgColor(object c);
        object GetBgColor();
        void SetFgColor(object c);
        object GetFgColor();
        void SetLinkColor(object c);
        object GetLinkColor();
        void SetVlinkColor(object c);
        object GetVlinkColor();
        string GetReferrer();
        /*later
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLLocation GetLocation();*/
        string GetLastModified();
        void SetUrl(string p);
        string GetUrl();
        void SetDomain(string p);
        string GetDomain();
        void SetCookie(string p);
        string GetCookie();
        void SetExpando(bool p);
        bool GetExpando();
        void SetCharset(string p);
        string GetCharset();
        void SetDefaultCharset(string p);
        string GetDefaultCharset();
        string GetMimeType();
        string GetFileSize();
        string GetFileCreatedDate();
        string GetFileModifiedDate();
        string GetFileUpdatedDate();
        string GetSecurity();
        string GetProtocol();
        string GetNameProp();
        int Write([In, MarshalAs(UnmanagedType.SafeArray)] object[] psarray);
        int WriteLine([In, MarshalAs(UnmanagedType.SafeArray)] object[] psarray);
        [return: MarshalAs(UnmanagedType.Interface)]
        object Open(string mimeExtension, object name, object features, object replace);
        void Close();
        void Clear();
        bool QueryCommandSupported(string cmdID);
        bool QueryCommandEnabled(string cmdID);
        bool QueryCommandState(string cmdID);
        bool QueryCommandIndeterm(string cmdID);
        string QueryCommandText(string cmdID);
        object QueryCommandValue(string cmdID);
        bool ExecCommand(string cmdID, bool showUI, object value);
        bool ExecCommandShowHelp(string cmdID);
        /*later
        IHTMLElement CreateElement(string eTag);*/
        void SetOnhelp(object p);
        object GetOnhelp();
        void SetOnclick(object p);
        object GetOnclick();
        void SetOndblclick(object p);
        object GetOndblclick();
        void SetOnkeyup(object p);
        object GetOnkeyup();
        void SetOnkeydown(object p);
        object GetOnkeydown();
        void SetOnkeypress(object p);
        object GetOnkeypress();
        void SetOnmouseup(object p);
        object GetOnmouseup();
        void SetOnmousedown(object p);
        object GetOnmousedown();
        void SetOnmousemove(object p);
        object GetOnmousemove();
        void SetOnmouseout(object p);
        object GetOnmouseout();
        void SetOnmouseover(object p);
        object GetOnmouseover();
        void SetOnreadystatechange(object p);
        object GetOnreadystatechange();
        void SetOnafterupdate(object p);
        object GetOnafterupdate();
        void SetOnrowexit(object p);
        object GetOnrowexit();
        void SetOnrowenter(object p);
        object GetOnrowenter();
        void SetOndragstart(object p);
        object GetOndragstart();
        void SetOnselectstart(object p);
        object GetOnselectstart();
        /*later
        IHTMLElement ElementFromPoint(int x, int y);*/
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLWindow2 GetParentWindow();
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetStyleSheets();
        void SetOnbeforeupdate(object p);
        object GetOnbeforeupdate();
        void SetOnerrorupdate(object p);
        object GetOnerrorupdate();
        string toString();
        [return: MarshalAs(UnmanagedType.Interface)]
        object CreateStyleSheet(string bstrHref, int lIndex);
    }






    [ComVisible(true), InterfaceType(ComInterfaceType.InterfaceIsDual), Guid("332C4427-26CB-11D0-B483-00C04FD90119")]
    public interface IHTMLWindow2
    {
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object Item([In] ref object pvarIndex);
        int GetLength();
        /*later
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLFramesCollection2 GetFrames();*/
        void SetDefaultStatus([In] string p);
        string GetDefaultStatus();
        void SetStatus([In] string p);
        string GetStatus();
        int SetTimeout([In] string expression, [In] int msec, [In] ref object language);
        void ClearTimeout([In] int timerID);
        void Alert([In] string message);
        bool Confirm([In] string message);
        [return: MarshalAs(UnmanagedType.Struct)]
        object Prompt([In] string message, [In] string defstr);
        object GetImage();
        /*later
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLLocation GetLocation();*/
        /*later
        [return: MarshalAs(UnmanagedType.Interface)]
        IOmHistory GetHistory();*/
        void Close();
        void SetOpener([In] object p);
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object GetOpener();
        [return: MarshalAs(UnmanagedType.Interface)]
        /*later
        IOmNavigator GetNavigator();
         * */
        void SetName([In] string p);
        string GetName();
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLWindow2 GetParent();
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLWindow2 Open([In] string URL, [In] string name, [In] string features, [In] bool replace);
        object GetSelf();
        object GetTop();
        object GetWindow();
        void Navigate([In] string URL);
        void SetOnfocus([In] object p);
        object GetOnfocus();
        void SetOnblur([In] object p);
        object GetOnblur();
        void SetOnload([In] object p);
        object GetOnload();
        void SetOnbeforeunload(object p);
        object GetOnbeforeunload();
        void SetOnunload([In] object p);
        object GetOnunload();
        void SetOnhelp(object p);
        object GetOnhelp();
        void SetOnerror([In] object p);
        object GetOnerror();
        void SetOnresize([In] object p);
        object GetOnresize();
        void SetOnscroll([In] object p);
        object GetOnscroll();
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLDocument2 GetDocument();
        /*later
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLEventObj GetEvent();*/
        object Get_newEnum();
        object ShowModalDialog([In] string dialog, [In] ref object varArgIn, [In] ref object varOptions);
        void ShowHelp([In] string helpURL, [In] object helpArg, [In] string features);
        /*later
        [return: MarshalAs(UnmanagedType.Interface)]
        IHTMLScreen GetScreen();*/
        object GetOption();
        void Focus();
        bool GetClosed();
        void Blur();
        void Scroll([In] int x, [In] int y);
        object GetClientInformation();
        int SetInterval([In] string expression, [In] int msec, [In] ref object language);
        void ClearInterval([In] int timerID);
        void SetOffscreenBuffering([In] object p);
        object GetOffscreenBuffering();
        [return: MarshalAs(UnmanagedType.Struct)]
        object ExecScript([In] string code, [In] string language);
        string toString();
        void ScrollBy([In] int x, [In] int y);
        void ScrollTo([In] int x, [In] int y);
        void MoveTo([In] int x, [In] int y);
        void MoveBy([In] int x, [In] int y);
        void ResizeTo([In] int x, [In] int y);
        void ResizeBy([In] int x, [In] int y);
        object GetExternal();
    }






    //Extend the WebBrowser control
    public class ExtendedWebBrowser : WebBrowser
    {

        // Define constants from winuser.h
        private const int WM_PARENTNOTIFY = 0x210;
        private const int WM_DESTROY = 2;

        AxHost.ConnectionPointCookie cookie;
        WebBrowserExtendedEvents events;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_PARENTNOTIFY:
                    if (!DesignMode)
                    {
                        if (m.WParam.ToInt32() == WM_DESTROY)
                        {
                            Message newMsg = new Message();
                            newMsg.Msg = WM_DESTROY;
                            // Tell whoever cares we are closing
                            Form parent = this.Parent as Form;
                            if (parent != null)
                                parent.Close();
                        }
                    }
                    DefWndProc(ref m);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        //This method will be called to give you a chance to create your own event sink
        protected override void CreateSink()
        {
            //MAKE SURE TO CALL THE BASE or the normal events won't fire
            base.CreateSink();
            events = new WebBrowserExtendedEvents(this);
            cookie = new AxHost.ConnectionPointCookie(this.ActiveXInstance, events, typeof(DWebBrowserEvents2));

        }

        public object Application
        {
            get
            {
                IWebBrowser2 axWebBrowser = this.ActiveXInstance as IWebBrowser2;
                if (axWebBrowser != null)
                {
                    return axWebBrowser.Application;
                }
                else
                    return null;
            }
        }

        public bool RegisterAsBrowser
        {
            get
            {
                IWebBrowser2 axWebBrowser = this.ActiveXInstance as IWebBrowser2;
                if (axWebBrowser != null)
                {
                    return axWebBrowser.RegisterAsBrowser;
                }
                else
                    return false;
            }
            set
            {
                IWebBrowser2 axWebBrowser = this.ActiveXInstance as IWebBrowser2;
                if (axWebBrowser != null)
                {
                    axWebBrowser.RegisterAsBrowser = true;
                }
            }
        }

        protected override void DetachSink()
        {
            if (null != cookie)
            {
                cookie.Disconnect();
                cookie = null;
            }
            base.DetachSink();
        }

        //This new event will fire for the NewWindow2
        public event EventHandler<NewWindow2EventArgs> NewWindow2;


        protected void OnNewWindow2(ref object ppDisp, ref bool cancel)
        {
            EventHandler<NewWindow2EventArgs> h = NewWindow2;
            NewWindow2EventArgs args = new NewWindow2EventArgs(ref ppDisp, ref cancel);
            if (null != h)
            {
                h(this, args);
            }
            /*IWebBrowser2 iwebBrowser2 = this.ActiveXInstance as IWebBrowser2;
            IHTMLDocument2 ihtmlwindowObj = iwebBrowser2.Document as IHTMLDocument2;
            if (ihtmlwindowObj!=null)
            {
                IHTMLWindow2 window = ihtmlwindowObj.GetParentWindow();
                window.SetOpener(
            }*/



            //Pass the cancellation chosen back out to the events
            //Pass the ppDisp chosen back out to the events
            cancel = args.Cancel;
            ppDisp = args.PPDisp;
        }

        //This new event will fire for the NewWindow3
        public event EventHandler<NewWindow3EventArgs> NewWindow3;


        protected void OnNewWindow3(ref object ppDisp, ref bool Cancel, uint dwFlags, string bstrUrlContext, string bstrUrl)
        {
            EventHandler<NewWindow3EventArgs> h = NewWindow3;
            NewWindow3EventArgs args = new NewWindow3EventArgs(ref ppDisp, ref Cancel, dwFlags, bstrUrlContext, bstrUrl);
            if (null != h)
            {
                h(this, args);
            }
            /*IWebBrowser2 iwebBrowser2 = this.ActiveXInstance as IWebBrowser2;
            IHTMLDocument2 ihtmlwindowObj = iwebBrowser2.Document as IHTMLDocument2;
            if (ihtmlwindowObj!=null)
            {
                IHTMLWindow2 window = ihtmlwindowObj.GetParentWindow();
                window.SetOpener(
            }*/



            //Pass the cancellation chosen back out to the events
            //Pass the ppDisp chosen back out to the events
            Cancel = args.Cancel;
            ppDisp = args.PPDisp;
        }


        //This new event will fire for the DocumentComplete
        public event EventHandler<DocumentCompleteEventArgs> DocumentComplete;

        protected void OnDocumentComplete(object ppDisp, object url)
        {
            EventHandler<DocumentCompleteEventArgs> h = DocumentComplete;
            DocumentCompleteEventArgs args = new DocumentCompleteEventArgs(ppDisp, url);
            if (null != h)
            {
                h(this, args);
            }
            //Pass the ppDisp chosen back out to the events
            ppDisp = args.PPDisp;
            //I think url is readonly
        }

        //This new event will fire for the DocumentComplete
        public event EventHandler<CommandStateChangeEventArgs> CommandStateChange;

        protected void OnCommandStateChange(long command, ref bool enable)
        {
            EventHandler<CommandStateChangeEventArgs> h = CommandStateChange;
            CommandStateChangeEventArgs args = new CommandStateChangeEventArgs(command, ref enable);
            if (null != h)
            {
                h(this, args);
            }
        }


        //This class will capture events from the WebBrowser
        public class WebBrowserExtendedEvents : System.Runtime.InteropServices.StandardOleMarshalObject, DWebBrowserEvents2
        {
            ExtendedWebBrowser _Browser;
            public WebBrowserExtendedEvents(ExtendedWebBrowser browser)
            { _Browser = browser; }

            //Implement whichever events you wish
            public void NewWindow2(ref object pDisp, ref bool cancel)
            {
                _Browser.OnNewWindow2(ref pDisp, ref cancel);
            }

            public void NewWindow3(ref object ppDisp, ref bool Cancel, uint dwFlags, string bstrUrlContext, string bstrUrl)
            {
                _Browser.OnNewWindow3(ref ppDisp, ref Cancel, dwFlags, bstrUrlContext, bstrUrl);
            }


            //Implement whichever events you wish
            public void DocumentComplete(object pDisp, ref object url)
            {
                _Browser.OnDocumentComplete(pDisp, url);
            }

            //Implement whichever events you wish
            public void CommandStateChange(long command, bool enable)
            {
                _Browser.OnCommandStateChange(command, ref enable);
            }

            public void WindowSetLeft(int Left)
            {
                ///Should I calculate any diff?
                _Browser.Parent.Left = Left;

            }

            public void WindowSetTop(int Top)
            {
                _Browser.Parent.Top = Top;

            }

            public void WindowSetWidth(int Width)
            {
                int diff = 0;
                diff = _Browser.Parent.Width - _Browser.Width;
                _Browser.Parent.Width = diff + Width;

            }
            public void WindowSetHeight(int Height)
            {
                int diff = 0;
                diff = _Browser.Parent.Height - _Browser.Height;
                _Browser.Parent.Height = diff + Height;

            }


        }
        [ComImport, Guid("34A715A0-6587-11D0-924A-0020AFC7AC4D"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch), TypeLibType(TypeLibTypeFlags.FHidden)]
        public interface DWebBrowserEvents2
        {
            [DispId(0x69)]
            void CommandStateChange([In] long command, [In] bool enable);
            [DispId(0x103)]
            void DocumentComplete([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp, [In] ref object URL);
            [DispId(0xfb)]
            void NewWindow2([In, Out, MarshalAs(UnmanagedType.IDispatch)] ref object pDisp, [In, Out] ref bool cancel);

            [DispId(0x108)]
            void WindowSetLeft(int Left);
            [DispId(0x109)]
            void WindowSetTop(int Top);
            [DispId(0x10a)]
            void WindowSetWidth(int Width);
            [DispId(0x10b)]
            void WindowSetHeight(int Height);

            [DispId(0x111)]
            void NewWindow3([In, Out, MarshalAs(UnmanagedType.IDispatch)] ref object ppDisp, [In, Out, MarshalAs(UnmanagedType.VariantBool)] ref bool Cancel, uint dwFlags, [MarshalAs(UnmanagedType.BStr)] string bstrUrlContext, [MarshalAs(UnmanagedType.BStr)] string bstrUrl);

        }

        [ComImport, Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E"), TypeLibType(TypeLibTypeFlags.FOleAutomation | TypeLibTypeFlags.FDual | TypeLibTypeFlags.FHidden)]
        public interface IWebBrowser2
        {
            [DispId(100)]
            void GoBack();
            [DispId(0x65)]
            void GoForward();
            [DispId(0x66)]
            void GoHome();
            [DispId(0x67)]
            void GoSearch();
            [DispId(0x68)]
            void Navigate([In] string Url, [In] ref object flags, [In] ref object targetFrameName, [In] ref object postData, [In] ref object headers);
            [DispId(-550)]
            void Refresh();
            [DispId(0x69)]
            void Refresh2([In] ref object level);
            [DispId(0x6a)]
            void Stop();
            [DispId(200)]
            object Application { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
            [DispId(0xc9)]
            object Parent { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
            [DispId(0xca)]
            object Container { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
            [DispId(0xcb)]
            object Document { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
            [DispId(0xcc)]
            bool TopLevelContainer { get; }
            [DispId(0xcd)]
            string Type { get; }
            [DispId(0xce)]
            int Left { get; set; }
            [DispId(0xcf)]
            int Top { get; set; }
            [DispId(0xd0)]
            int Width { get; set; }
            [DispId(0xd1)]
            int Height { get; set; }
            [DispId(210)]
            string LocationName { get; }
            [DispId(0xd3)]
            string LocationURL { get; }
            [DispId(0xd4)]
            bool Busy { get; }
            [DispId(300)]
            void Quit();
            [DispId(0x12d)]
            void ClientToWindow(out int pcx, out int pcy);
            [DispId(0x12e)]
            void PutProperty([In] string property, [In] object vtValue);
            [DispId(0x12f)]
            object GetProperty([In] string property);
            [DispId(0)]
            string Name { get; }
            [DispId(-515)]
            int HWND { get; }
            [DispId(400)]
            string FullName { get; }
            [DispId(0x191)]
            string Path { get; }
            [DispId(0x192)]
            bool Visible { get; set; }
            [DispId(0x193)]
            bool StatusBar { get; set; }
            [DispId(0x194)]
            string StatusText { get; set; }
            [DispId(0x195)]
            int ToolBar { get; set; }
            [DispId(0x196)]
            bool MenuBar { get; set; }
            [DispId(0x197)]
            bool FullScreen { get; set; }
            [DispId(500)]
            void Navigate2([In] ref object URL, [In] ref object flags, [In] ref object targetFrameName, [In] ref object postData, [In] ref object headers);
            [DispId(0x1f7)]
            void ShowBrowserBar([In] ref object pvaClsid, [In] ref object pvarShow, [In] ref object pvarSize);
            [DispId(-525)]
            WebBrowserReadyState ReadyState { get; }
            [DispId(550)]
            bool Offline { get; set; }
            [DispId(0x227)]
            bool Silent { get; set; }
            [DispId(0x228)]
            bool RegisterAsBrowser { get; set; }
            [DispId(0x229)]
            bool RegisterAsDropTarget { get; set; }
            [DispId(0x22a)]
            bool TheaterMode { get; set; }
            [DispId(0x22b)]
            bool AddressBar { get; set; }
            [DispId(0x22c)]
            bool Resizable { get; set; }
        }


    }

}
