// 
// LuaTable.cs
//  
// Author:
//       Joshua Simmons <simmons.44@gmail.com>
// 
// Copyright (c) 2009 Joshua Simmons
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

// 
// LuaFunction.cs
//  
// Author:
//       Joshua Simmons <simmons.44@gmail.com>
// 
// Copyright (c) 2009 Joshua Simmons
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using LuaWrap;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LuaSharp
{
	public class LuaTable : IDisposable
	{
		private IntPtr state;
		internal int reference;
		
		/// <summary>
		/// Creates a LuaFunction for the object on the top of the stack, and pops it.
		/// </summary>
		/// <param name="s">
		/// A Lua State
		/// </param>
		public LuaTable( IntPtr s )
		{
			state = s;
			reference = LuaLib.luaL_ref( state, (int)PseudoIndex.Registry );
		}		
		
		public void Dispose()
		{
			if( reference == (int)References.NoRef )
				return;

			LuaLib.luaL_unref( state, (int)PseudoIndex.Registry, reference );
			reference = (int)References.NoRef;
			
			System.GC.SuppressFinalize( this );
		}

		public object this[object path]
		{
			get
			{
				return GetValue( path );
			}
			set
			{
				SetValue( path, value );
			}
		}
		
		public object GetValue( object key )
		{		
			if( key == null )
				throw new ArgumentNullException( "key" );

			LuaLib.luaL_getref( state, (int)PseudoIndex.Registry, reference );
	
			Helpers.Push( state, key );
			LuaLib.lua_gettable( state, -2 );
			return Helpers.Pop( state );
		}
		
		public void SetValue( object key, object value )
		{
			if( key == null )
				throw new ArgumentNullException( "key" );

			LuaLib.luaL_getref( state, (int)PseudoIndex.Registry, reference );
	
			Helpers.Push( state, key );
			Helpers.Push( state, value );
			LuaLib.lua_settable( state, -3 );
		}

        public ListDictionary GetTableDict()
        {
            ListDictionary dict = new ListDictionary();

            int oldTop = LuaLib.lua_gettop(state);
            Helpers.Push(state, this);
            LuaLib.lua_pushnil(state);
            while (LuaLib.lua_next(state, -2) != false)
            {
                dict.Add( Helpers.GetObject(state, -2),Helpers.GetObject(state, -1 ) );
                LuaLib.lua_settop(state, -2);
            }
            LuaLib.lua_settop(state, oldTop);

            return dict;
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return GetTableDict().GetEnumerator();
        }

        //TODO: overload ToString
        public string MyToString()
        {
            String tableStr = "";
            IDictionaryEnumerator ienum = (IDictionaryEnumerator)this.GetEnumerator();
            while (ienum.MoveNext())
            {
                if (tableStr.Length > 0)
                {
                    //dont do it the first time
                    tableStr += ", ";
                }
                else
                {
                    tableStr += "{";
                }
                tableStr += Environment.NewLine;

                Object key = ienum.Key;
                Object val = ienum.Value;

                tableStr += "[";
                if (key.GetType() == typeof(Double))
                {
                    tableStr += key;
                }
                else
                {
                    tableStr += "\"" + key + "\"";
                }
                tableStr += "] = ";

                String valStr = "";
                if (ienum.Value.GetType() == typeof(LuaTable))
                {
                    valStr += (val as LuaTable).MyToString();
                }
                else if (ienum.Value.GetType() == typeof(String))
                {
                    string data = Regex.Escape(val as String); //preserver escape symbols
                    valStr += "\"" + data + "\"";
                }
                else if (ienum.Value.GetType() == typeof(Double))
                {
                    Double d = (Double)val;
                    valStr += d.ToString(CultureInfo.CreateSpecificCulture("en-US").NumberFormat );
                }
                else if (ienum.Value.GetType() == typeof(Boolean))
                {
                    Boolean b = (Boolean)val;
                    if (b)
                    {
                        valStr += "true";
                    }
                    else
                    {
                        valStr += "false";
                    }
                }
                else
                {
                    //unknown data type here
                    //System.Console.WriteLine(ienum.Value.GetType().ToString());                    
                    valStr += val;
                }

                if (valStr.Length == 0)
                {
                    valStr = "{}";
                }

                tableStr += valStr;
            }

            if (tableStr.Length > 0)
            {
                tableStr += Environment.NewLine;
                tableStr += "}";
            }

            return tableStr;
        }
	}
}
