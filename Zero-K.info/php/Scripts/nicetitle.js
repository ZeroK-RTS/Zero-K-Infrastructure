// adds hover tooltip for "title" atributes on elements a, div, span, th and input. Its styled using nicetitle css class
// if tooltip text starts with "$", it gets tooltip asynchronously from AJAX  - calls Home.mvc/GetTooltip(key)  
// if url in hyperlink contains dialog_mode=1, creates windows_open command for it

addEvent(window, "load", makeNiceTitles);
window.setInterval(makeNiceTitles, 1000);

var XHTMLNS = "http://www.w3.org/1999/xhtml";
var CURRENT_NICE_TITLE;
var browser = new Browser();


function openDialog(e) {
	if (window.event && window.event.srcElement) {
		lnk = window.event.srcElement
	} else if (e && e.target) {
		lnk = e.target
	}

	while (lnk.tagName != 'A' && lnk.parentNode) lnk = lnk.parentNode; // find correct node, event can be invoked on subnode

  // remove all invalid characters from url to produce target
	var url = lnk.getAttribute("origHref");
	var target = "";
	for (var i = 0; i < url.length; i++) {
		if (!((url[i] >= 'a' && url[i] <= 'z') || (url[i] >= 'A' && url[i] <= 'Z') || (url[i] >= '0' && url[i] <= '9'))) {
			target = target + '_';
		} else target = target + url[i];
	}
	window.open(url, target, "scrollbars=1,menubar=0,resizable=1,toolbars=0,height=200,width=200").focus(); // opera needs left and top or it wont display in popup
		
	return false;
}


function makeNiceTitles() {
	if (!document.createElement || !document.getElementsByTagName) return;
	
	// add namespace methods to HTML DOM; this makes the script work in both
	// HTML and XML contexts.
	if (!document.createElementNS) {
		document.createElementNS = function(ns, elt) {
			return document.createElement(elt);
		}
	}

	
	var myelem = document.getElementsByTagName("span");
	for (var ti = 0; ti < myelem.length; ti++) {
		var lnk = myelem[ti];
		if (lnk.title) {
			lnk.setAttribute("nicetitle", lnk.title);
			lnk.removeAttribute("title");
			addEvent(lnk, "mouseover", showNiceTitle);
			addEvent(lnk, "mouseout", hideNiceTitle);
			addEvent(lnk, "focus", showNiceTitle);
			addEvent(lnk, "blur", hideNiceTitle);
		}
	}


	var myelem = document.getElementsByTagName("td");
	for (var ti = 0; ti < myelem.length; ti++) {
		var lnk = myelem[ti];
		if (lnk.title) {
			lnk.setAttribute("nicetitle", lnk.title);
			lnk.removeAttribute("title");
			addEvent(lnk, "mouseover", showNiceTitle);
			addEvent(lnk, "mouseout", hideNiceTitle);
			addEvent(lnk, "focus", showNiceTitle);
			addEvent(lnk, "blur", hideNiceTitle);
		}
	}

	var myelem = document.getElementsByTagName("div");
	for (var ti = 0; ti < myelem.length; ti++) {
		var lnk = myelem[ti];
		if (lnk.title) {
			lnk.setAttribute("nicetitle", lnk.title);
			lnk.removeAttribute("title");
			addEvent(lnk, "mouseover", showNiceTitle);
			addEvent(lnk, "mouseout", hideNiceTitle);
			addEvent(lnk, "focus", showNiceTitle);
			addEvent(lnk, "blur", hideNiceTitle);
		}
	}

	var myelem = document.getElementsByTagName("a");
	for (var ti = 0; ti < myelem.length; ti++) {
		var lnk = myelem[ti];

		var dpos = lnk.href.indexOf("dialog_mode=1");
		if (lnk.href && dpos != -1) {
			lnk.setAttribute("origHref", lnk.href.replace("dialog_mode=1","is_dialog=1"));
			addEvent(lnk, "click", openDialog);
			lnk.href = "#";
		}

		if (lnk.title) {
			lnk.setAttribute("nicetitle", lnk.title);
			lnk.removeAttribute("title");
			addEvent(lnk, "mouseover", showNiceTitle);
			addEvent(lnk, "mouseout", hideNiceTitle);
			addEvent(lnk, "focus", showNiceTitle);
			addEvent(lnk, "blur", hideNiceTitle);
		}
	}

	/*var myelem = document.getElementsByTagName("th");
	for (var ti = 0; ti < myelem.length; ti++) {
	var lnk = myelem[ti];
	if (lnk.title) {
	var store = lnk.title;
	lnk.setAttribute("nicetitle", lnk.title);
	lnk.removeAttribute("title");
	addEvent(lnk, "mouseover", showNiceTitle);
	addEvent(lnk, "mouseout", hideNiceTitle);
	addEvent(lnk, "focus", showNiceTitle);
	addEvent(lnk, "blur", hideNiceTitle);
	var childelem = lnk.childNodes;
	for (var tc = 0; tc < childelem.length; tc++) {
	var ch = childelem[tc];
	if (ch.attributes != null) {
	ch.setAttribute("nicetitle", store);
	addEvent(ch, "mouseover", showNiceTitle);
	addEvent(ch, "mouseout", hideNiceTitle);
	addEvent(ch, "focus", showNiceTitle);
	addEvent(ch, "blur", hideNiceTitle);
	}
	}
	}
	}*/

	var myelem = document.getElementsByTagName("input");
	for (var ti = 0; ti < myelem.length; ti++) {
		var lnk = myelem[ti];
		if (lnk.title) {
			lnk.setAttribute("nicetitle", lnk.title);
			lnk.removeAttribute("title");
			addEvent(lnk, "mouseover", showNiceTitle);
			addEvent(lnk, "mouseout", hideNiceTitle);
			addEvent(lnk, "focus", showNiceTitle);
			addEvent(lnk, "blur", hideNiceTitle);
		}
	}
}

function findPosition(oLink) {
	if (oLink.offsetParent) {
		for (var posX = 0, posY = 0; oLink.offsetParent; oLink = oLink.offsetParent) {
			posX += oLink.offsetLeft;
			posY += oLink.offsetTop;
		}
		return [posX, posY];
	} else {
		return [oLink.x, oLink.y];
	}
}

var cachedTooltips = new Array(); // stores cached asynchronous tooltips

function showNiceTitle(e) {
	if (CURRENT_NICE_TITLE) hideNiceTitle(CURRENT_NICE_TITLE);
	if (!document.getElementsByTagName) return;
	if (window.event && window.event.srcElement) {
		lnk = window.event.srcElement
	} else if (e && e.target) {
		lnk = e.target
	}

	if (!lnk) return;

	while (!lnk.getAttribute("nicetitle") && lnk.parentNode) lnk = lnk.parentNode; // find correct node, event can be invoked on subnode
	
	if (lnk.nodeType == 3) {
		// lnk is a textnode -- ascend parents until we hit a link
		lnk = getParent(lnk, "A");
	}
	if (!lnk) lnk = getParent(lnk, "span");
	if (!lnk) return;
	nicetitle = lnk.getAttribute("nicetitle");

	if (nicetitle == null || nicetitle == "") return;

	var d = document.createElementNS(XHTMLNS, "div");
	d.className = "nicetitle";


	w = d.style.width;
	d.style.left = (lnk.getBoundingClientRect().right +10) + document.documentElement.scrollLeft + "px";
	d.style.top = (lnk.getBoundingClientRect().top + 10) + document.documentElement.scrollTop + "px";

	document.body.appendChild(d);
	CURRENT_NICE_TITLE = d;

	var asyncMode = nicetitle.charAt(0) == '$';
	if (!asyncMode) writeCode(nicetitle, d);
	else  {
		if (cachedTooltips[nicetitle] != null) {
			writeCode(cachedTooltips[nicetitle], d);
		} else {
			
			writeCode("....", d); // we write nothing in async mode - wait for data request

			var context = CURRENT_NICE_TITLE;
			
			$.get('/Home.mvc/GetTooltip?key=' + nicetitle, function (ret) {
				cachedTooltips[nicetitle] = ret;
				if (context == CURRENT_NICE_TITLE) { // if tooltip still same, update it
					CURRENT_NICE_TITLE.removeChild(CURRENT_NICE_TITLE.childNodes[0]); // remove previous
					writeCode(ret, CURRENT_NICE_TITLE);
				}
			});
		}
	}
}


function hideNiceTitle(e) {
	if (!document.getElementsByTagName) return;
	if (CURRENT_NICE_TITLE) {
		document.body.removeChild(CURRENT_NICE_TITLE);
		CURRENT_NICE_TITLE = null;
	}
}

// Add an eventListener to browsers that can do it somehow.
// Originally by the amazing Scott Andrew.
function addEvent(obj, evType, fn) {
	if (obj.addEventListener) {
		obj.addEventListener(evType, fn, false);
		return true;
	} else if (obj.attachEvent) {
		var r = obj.attachEvent("on" + evType, fn);
		return r;
	} else {
		return false;
	}
}

function getParent(el, pTagName) {
	if (el == null) return null;
	else if (el.nodeType == 1 && el.tagName.toLowerCase() == pTagName.toLowerCase())	// Gecko bug, supposed to be uppercase
		return el;
	else
		return getParent(el.parentNode, pTagName);
}

function getMousePosition(event) {
	if (browser.isIE) {
		x = window.event.clientX + document.documentElement.scrollLeft
      + document.body.scrollLeft;
		y = window.event.clientY + document.documentElement.scrollTop
      + document.body.scrollTop;
	}
	if (browser.isNS) {
		x = event.clientX + window.scrollX;
		y = event.clientY + window.scrollY;
	}
	return [x, y];
}

// Determine browser and version.

function Browser() {
	// blah, browser detect, but mouse-position stuff doesn't work any other way
	var ua, s, i;

	this.isIE = false;
	this.isNS = false;
	this.version = null;

	ua = navigator.userAgent;

	s = "MSIE";
	if ((i = ua.indexOf(s)) >= 0) {
		this.isIE = true;
		this.version = parseFloat(ua.substr(i + s.length));
		return;
	}

	s = "Netscape6/";
	if ((i = ua.indexOf(s)) >= 0) {
		this.isNS = true;
		this.version = parseFloat(ua.substr(i + s.length));
		return;
	}

	// Treat any other "Gecko" browser as NS 6.1.

	s = "Gecko";
	if ((i = ua.indexOf(s)) >= 0) {
		this.isNS = true;
		this.version = 6.1;
		return;
	}

	this.isNS = true;
	this.version = 6.1;
	return;
}

// ----------------------------------
// DOM parser - needed to convert code text in title to HTML DOM
// ----------------------------------
function writeCode(s, pointer) {
	var parent, tag, j;
	for (var i = 0; i < s.length; i++) {
		var c = s.charAt(i);
		if (c == "<") {
			var j = s.indexOf(">", i + 1);
			tag = s.substring(i, j + 1);
			if (tag.charAt(tag.length - 2) == "/") {  // empty element 
				if (pointer != null && pointer.nodeType == 3) { pointer = pointer.parentNode; }
				pointer.appendChild(createElementFromString(tag));
			} else if (tag.charAt(1) != "/") {  // normal open tag 
				if (pointer != null && pointer.nodeType == 3) { pointer = pointer.parentNode; }
				pointer = pointer.appendChild(createElementFromString(tag));
			} else {  // must be a closing tag 
				if (pointer.parentNode != null && pointer.parentNode.parentNode != null)
					pointer = pointer.parentNode.parentNode;
			}
			i = j;
		} else {
			if (pointer.nodeType == 3) {
				n = s.indexOf("<", i + 1);
				if (n == -1) {
					pointer.nodeValue += s.substr(i);  // textNode.appendData not implemented in IE 5.x   
					i = s.length;
				} else {
					pointer.nodeValue += s.substring(i, n);
					i = n - 1;
				}
			} else {
				pointer = pointer.appendChild(document.createTextNode(c));
			}
		}
	}
}


function createElementFromString(str) {
	var node, a = str.match(/<(\w+)(\s+)?([^>]+)?>/);  // split into name and key/value pairs 
	if (a != null) {
		node = document.createElement(a[1]);
		if (a[3] != null) {
			var attrs = a[3].split(" ");  // split the key/value pairs 
			if (attrs.length > 0) {
				for (var i = 0; i < attrs.length; i++) {
					var att = attrs[i].split("=");  // split a key/value pair 
					if (att[0].length > 0 && att[0] != "/" && att[1].length != 2) {  // do not operate on empty attributes 
						var a_n = document.createAttribute(att[0]);
						a_n.value = att[1].replace(/^['"](.+)['"]$/, "$1");  // remove quotes from attribute value 
						node.setAttributeNode(a_n);  // append attribute node to new node 
					}
				}
			}
		}
	}
	return node;
}