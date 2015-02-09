using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
	/// <summary>
	/// Contain characters used by TableReader to parse table (default character set is for LUA table).
	/// </summary>
	public class TableReaderConfig
	{
		public String prefix="id_";
		public String stringGlue = "...";
		public char[] stringChars = new char[2]{'\'','"'};
        public String blockStringOpen = "[[";
        public String blockStringClose = "]]";
        public String blockCommentOpen = "--[[";
        public String blockCommentClose = "--]]";
        public String commentString = "--";
        public char tableOpen = '{';
        public char tableClose = '}';
        public char contentSeparator = ',';
        public char[] newLineChar = {'\n','\r'};
        public char whitespaceA = ' ';
        public char whitespaceB = '\t';
        public char escapeCharSign = '\\';
        public char equalSign = '=';
        
        public TableReaderConfig()
        {
        }
	}
	
	/// <summary>
	/// Tool for parsing Spring related table.
	/// </summary>
	public static class TableReader
	{
        /// <summary>
        /// Tbis function convert the content of a table inside an input text into a Dictionary(String,Object) object.
		/// The parsing start from the first bracket '{' and end at the closing bracket '}'		
		/// The search for bracket pairs "{}" start from argument "startIndex".
		/// The function will output the index of closing bracket '}' as variable "offset" when it finishes.
		/// The value in Dictionary(String,Object) will be another Dictionary(String,Object) when its a nexted table, or String when its normal value.
		/// Note: This reader don't parse function and return value, and dont pre-calculate any math statement embedded in LUA table (will fail).
		/// Example usage is in SkirmishControlTool.cs. Any improvement & simplification of this function is welcomed!
		/// Currently able to extract content of Spring related LUA config files (including able to handle commented line) & Spring Startscript (use ';' as content saperator)
		/// </summary>
        public static Dictionary<String,Object> ParseTable(TableReaderConfig config, int startIndex, String text, String filePath, out int offset)
        {
            var contentList =new Dictionary<String,Object>();
            String prefix= config.prefix;
            
            String stringGlue = config.stringGlue;
            
            int stringCharType = 0;
            char[] stringChars = config.stringChars;
            bool blockStringChar = false;
            
            int blockStringCount = 0;
            String blockStringOpen = config.blockStringOpen;
            String blockStringClose = config.blockStringClose;
            bool blockStringString = false;
            
            int blockCommentCount = 0;
            String blockCommentOpen = config.blockCommentOpen;
            String blockCommentClose = config.blockCommentClose;
            bool blockComment = false;
            
            int commentCharCount = 0;
            String commentString = config.commentString;
            bool lineComment = false;
            
            bool inTable = false;
            char tableOpen = config.tableOpen;
            char tableClose = config.tableClose;
            
            char contentSeparator = config.contentSeparator;
            int contentIndex = 0;
            
            char[] newLineChar = config.newLineChar;
            char whitespaceA = config.whitespaceA;
            char whitespaceB = config.whitespaceB;
            
            String capturedValue1 = "";
            String capturedValue2 = "";
            Object capturedObject1 = null;
            char escapeCharSign = config.escapeCharSign;
            bool isEscapeCharNow = false;
            bool detectedUnspacedChar = false;
            
            bool detectedEqualSign = false;
            char equalSign = config.equalSign;
            
            int i=startIndex;
            while(i<text.Length)
            {
                //string block, [[ ]] " '
                if (!lineComment && !blockComment)
                {
                    if (blockStringString || blockStringChar)
                    {
                        //escape char for displaying " and ' char in string area
                        if(text[i]==escapeCharSign)
                        {
                            if (!isEscapeCharNow)
                            {
                                blockStringCount = 0;
                                
                                isEscapeCharNow=true;
                                
                                i++;
                                continue;
                            }
                            isEscapeCharNow=false;
                        }
                    }
                    
                    if (!isEscapeCharNow && !blockStringChar)
                    {
                        if (blockStringString)
                        {
                            if (text[i]==blockStringClose[blockStringCount])
                            {                                
                                blockStringCount++;
                                if (blockStringCount==blockStringClose.Length)
                                {
                                    blockStringString=false;
                                    blockStringCount=0;
                                    
                                    UndoCaptureThisChar(blockStringClose[0],detectedEqualSign,ref capturedValue1,ref capturedValue2);

                                    i++;
                                    continue;
                                }
                            }else
                                blockStringCount=0;
                        }else
                        {
                            if (text[i]==blockStringOpen[blockStringCount])
                            {    
                                blockStringCount++;
                                if (blockStringCount==blockStringOpen.Length)
                                {
                                    blockStringString=true;
                                    blockStringCount=0;
                                    
                                    UndoCaptureThisChar(blockStringOpen[0],detectedEqualSign,ref capturedValue1,ref capturedValue2);

                                    i++;
                                    continue;
                                }
                            }else
                                blockStringCount=0;
                        }
                    }
                    
                    if (!isEscapeCharNow && !blockStringString)
                    {
                        if(blockStringChar)
                        {
                            if (text[i]==stringChars[stringCharType])
                            {
                                blockStringChar=false;
                                
                                i++;
                                continue;
                            }
                        }else
                        {
                            if (text[i]==stringChars[0])
                            {
                                blockStringChar=true;
                                stringCharType = 0;
                                
                                i++;
                                continue;
                            }else if (text[i]==stringChars[1])
                            {
                                blockStringChar=true;
                                stringCharType = 1;
                                
                                i++;
                                continue;
                            }
                        }
                    }
                    
                    if (blockStringString || blockStringChar)
                    {
                        char newChar = text[i];
                        
                        if (isEscapeCharNow && text[i]=='n') //newline
                            newChar = '\n';
                        isEscapeCharNow = false;
                        
                        CaptureChar(newChar,detectedEqualSign,ref capturedValue1,ref capturedValue2);
                        
                        i++;
                        continue;
                    }
                }
                
                //connector between string value, "..."
                if (!detectedUnspacedChar && text[i]==stringGlue[0])
                {
                    i++;
                    continue;
                }
                
                //Whitespace
                if (text[i]==whitespaceA || text[i]==whitespaceB)
                {
                    detectedUnspacedChar = false;
                    
                    i++;
                    continue;
                }
                
                //Newline
                if (text[i]==newLineChar[0] || text[i]==newLineChar[1])
                {
                    lineComment = false;
                    
                    i++;
                    continue;
                }
                
                //Block comment
                if (!blockComment)
                {    
                    if (text[i]==blockCommentOpen[blockCommentCount])
                    {
                        blockCommentCount++;
                        if (blockCommentCount==blockCommentOpen.Length)
                        {
                            blockComment=true;
                            blockCommentCount=0;
                            
                            i++;
                            continue;
                        }
                    }else
                        blockCommentCount=0;
                }else
                {
                    if (text[i]==blockCommentClose[blockCommentCount])
                    {
                        blockCommentCount++;
                        if (blockCommentCount==blockCommentClose.Length)
                        {
                            blockComment=false;
                            blockCommentCount=0;
                            
                            i++;
                            continue;
                        }
                    }else
                        blockCommentCount=0;
                    
                    i++;
                    continue;
                }
                
                //Line comment, --
                if(!lineComment)
                {
                    if (text[i]==commentString[commentCharCount])
                    {
                        commentCharCount++;
                        if (commentCharCount==commentString.Length)
                        {
                            lineComment=true;
                            commentCharCount=0;
                            
                            UndoCaptureThisChar(commentString[0],detectedEqualSign,ref capturedValue1,ref capturedValue2);
                            
                            i++;
                            continue;
                        }
                    }else
                        commentCharCount=0;
                }else
                {
                    i++;
                    continue;
                }
                
                //{ and }
                if (!inTable && text[i]==tableOpen)
                {
                    inTable = true;
                    
                    i++;
                    continue;
                }
                else if (text[i]==tableOpen)
                {
                    int offsetIn = 0;
                    var list = ParseTable(config,i,text,filePath,out offsetIn);
                    capturedObject1=list;
                    
                    bool saved = SaveKeyValuePair(prefix,contentIndex,capturedValue1,capturedValue2,capturedObject1,filePath,detectedEqualSign,ref contentList);
                    
                    capturedValue1 = "";
			        capturedValue2 = "";
			        capturedObject1 = null;
			        detectedEqualSign = false;
			        
			        if (saved) contentIndex++;
                    
                    i = i+offsetIn;
                    continue;
                }
                else if (text[i]==tableClose)
                {                 
                    SaveKeyValuePair(prefix,contentIndex,capturedValue1,capturedValue2,capturedObject1,filePath,detectedEqualSign,ref contentList);
                    
                    inTable = false;
                    offset = (i-startIndex)+1; //is out
                    return contentList;
                }
                if (!inTable)
                {
                    i++;
                    continue;
                }
                
                //content separator, ","
                if (text[i]==contentSeparator)
                {                  
                    bool saved = SaveKeyValuePair(prefix,contentIndex,capturedValue1,capturedValue2,capturedObject1,filePath,detectedEqualSign,ref contentList);
                            
			        capturedValue1 = "";
			        capturedValue2 = "";
			        capturedObject1 = null;
			        detectedEqualSign = false;
			        
			        if (saved) contentIndex++;
	        
                    i++;
                    continue;
                }
                
                //key value separator, =
                if (text[i]==equalSign)
                {
                    detectedEqualSign = true;
                    
                    i++;
                    continue;
                }
                
                detectedUnspacedChar = true;
                
                CaptureChar(text[i],detectedEqualSign,ref capturedValue1,ref capturedValue2);

                i++;
            }
            offset = text.Length;
            return contentList;
        }
        
        private static void CaptureChar(char toCapture, bool detectedEqualSign,ref String capturedValue1,ref String capturedValue2)
        {
            if (detectedEqualSign)
                capturedValue2 = capturedValue2 + toCapture;
            else
                capturedValue1 = capturedValue1 + toCapture;
        }
        
        private static void UndoCaptureThisChar(char charToUndo,bool detectedEqualSign,ref String capturedValue1,ref String capturedValue2)
        {
            if (detectedEqualSign)
                capturedValue2 = capturedValue2.TrimEnd(charToUndo);
            else
                capturedValue1 = capturedValue1.TrimEnd(charToUndo);
        }

        private static bool SaveKeyValuePair(String prefix,int contentIndex,String capturedValue1,String capturedValue2,Object capturedObject1,String filePath, bool detectedEqualSign, ref Dictionary<String,Object> contentList)
        {
        	capturedValue1 = capturedValue1.Trim(new char[2]{'[',']'});
        	
        	if (!detectedEqualSign) //didn't explicitly mention key valu pair
            {
        		if (capturedValue1!="" && capturedObject1!=null) //Spring-demo's syntax of a table's name
        		{
        			//do nothing
        		}
        		else
        		{
              		capturedValue2 = capturedValue1;
              		capturedValue1 = prefix + contentIndex;
        		}
            }
        	
            if (capturedValue2!="" || capturedObject1!=null)
            {   
                bool duplicate = false;
                if(contentList.ContainsKey(capturedValue1))
                {
                    duplicate = true;
                    System.Diagnostics.Trace.TraceWarning("CrudeLUAReader: detected duplicate value in " + filePath + " : " + capturedValue1 + "="+ contentList[capturedValue1]);
                }

                if (capturedObject1==null)
                {
                    if(duplicate) 
                        contentList[capturedValue1] = capturedValue2;
                    else
                        contentList.Add(capturedValue1,capturedValue2);
                }
                else
                {
                    if(duplicate)
                        contentList[capturedValue1]=capturedObject1;
                    else
                        contentList.Add(capturedValue1,capturedObject1);
                }
                return true;
            }
            return false;
        }
	}
}
