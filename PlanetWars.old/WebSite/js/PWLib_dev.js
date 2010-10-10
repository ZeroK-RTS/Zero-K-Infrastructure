(function() {

var PWlib = function() {

	var self = this;

	
	this.utils = {
		uidcount: 0,
		GenerateUID: function() {
			this.uidcount++;
			var uid = "pwlid_N" + this.uidcount + "_Q" + Math.round(Math.random() * 100000);
			return uid;
		}
	};
	
	this.SetImageSourceOnClick = function (id, source) {
		$(id).click(function(e) {
			this.attr("src", source);
		});
	};
	
	this.Dialog = function(html, opts) {
		
		this.elements = {};
		this.options  = {};
	
		
		this.Create = function(html, usropts) {
			var options = {
				bgOpacity: 50,
				overlayId: "pwDialogOverlay",
				windowId: "pwDialogWindow",
				windowCss: {}
			};
			$.extend(options, usropts);
			this.options = options;
			
			this.elements.overlay = $('<div></div>')
				.attr("id", this.options.overlayId)
				.css({
					opacity: this.options.bgOpacity / 100,
					width: '100%',
					height: '100%',
					position: 'fixed',
					margin: 0,
					padding: 0,
					left: 0,
					top: 0,
					zIndex: 3000,
					backgroundColor: '#000'
				})
				.appendTo('body')
				.hide()
				.fadeIn('slow');
				
			this.elements.window = $('<div></div>')
				.attr("id", this.options.windowId)
				.css($.extend(this.options.windowCss, {
					zIndex:  3100,
					position: 'fixed'
				}))
				.append(html)
				.appendTo('body')
				.hide()
				.fadeIn('slow');
		};
		
		this.Close = function() {
			this.elements.window.remove();
			this.elements.overlay.remove();
		}
		
		this.Create(html, opts);
	};
	
	this.AddTooltip = function(id, text, usropt) {
	
		var d = $("#" + id);
		if (!d)
			return false;
		if (!options)
			options = {unused: true};
		if (!text)
			text = "<img src=\"http://planetwars.licho.eu/minimaps/Dead%20Reef%20Dry.smf.jpg\" width=\"300\" height=\"150\" /><br />Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Suspendisse blandit convallis magna. Integer ultricies libero sit amet orci. Vivamus neque pede, hendrerit sit amet, congue at, vulputate commodo, arcu. Etiam sit amet nisl at arcu eleifend cursus. Praesent accumsan, mauris sed placerat pulvinar, felis erat vehicula dui, non lacinia ante ipsum vitae leo. Suspendisse in nibh a tellus consectetuer lacinia. Ut orci est, hendrerit eget, malesuada rutrum, blandit sed, mauris. Curabitur auctor mattis ante. Suspendisse ultricies. Nunc hendrerit mi ac odio. Duis volutpat rutrum felis. Mauris fermentum, lorem et imperdiet tempus, nulla purus pellentesque diam, fringilla tempor justo sem in quam. Nulla facilisi. Integer convallis est at elit. Nulla vel arcu. Praesent vitae nisl eu tortor lobortis suscipit. Nullam vulputate libero et augue. ";
			
		var options = {
			OffsetX: 25,
			OffsetY: 2,
			myclass: "",
			image: {
				src: "http://google.com/google.jpg",
				width: 354,
				height: 116
			}
		};
		
		$.extend(options, usropt);
		var uid = self.utils.GenerateUID();
		/*
		var ttdiv = $("<div id=\""+uid+"\">" +
		              "<img src=\"" +options.image.src+"\" width=\""+options.image.width+"\" height=\""+options.image.height+"\"" + 
		              "alt=\"No Image\" />" +
		              "<div style=\"float: right; width: 300px;\">"+text+"</div></div>");
		*/
		var ttdiv = $("<div id=\""+uid+"\">" +
		              "<div>" + text + "</div></div");
		
		ttdiv.appendTo($('body'))
		     .addClass(options.myclass, "")
			 .hide();
		
		// MOUSE OVER
		d.hover(function(e) {
			
			ttdiv.stop().show()
			     .css("position", "absolute")
				 .css("top", (e.pageY + options.OffsetY) + "px")
				 .css("left", (e.pageX + options.OffsetX) + "px")
				 .css("z-index", 10)
				 .show();
				 
		// MOUSE OUT
		}, function() {
			ttdiv.css({display: 'none'});
		});
		
		d.mousemove(function(e) {
			ttdiv.css("top", e.pageY + options.OffsetY + "px")
				 .css("left", e.pageX + options.OffsetX + "px");
		});
	
	};
	
	this.graphics = {
		
		
		uid: "undQI001",
		color: "#0000ff",
		stroke: 3,
		thtm: "",
		canvas: false,
		canvasDIV: false,
		lineCount: 0,

		init: function() {
			this.uid = self.utils.GenerateUID();
			this.canvas = $("<div id=\""+this.uid+"\"></div>");
			this.canvas.appendTo('body');
			this.canvasDIV = document.getElementById(this.uid);
		},
	
		_MakeDiv: function(x, y, w, h, z) {
			this.lineCount++;
			//this.thtm += '<div style="z-index:'+z+'; position:absolute; left:'+x+'px; top:'+y+'px; width:'+w+'px; height:'+h+'px; background-color: '+this.color+';"></div>';
			this.thtm += ['<div style="z-index:',z,'; position:absolute; left:',x,'px; top:',y,'px; width:',w,'px; height:',h ,'px; background-color: ',this.color,';"></div>'].join('');
		},
		
		SetStroke: function(size) {
			this.stroke = size;
		},
		
		SetColor: function(str) {
			this.color = str;
		},
		
		Paint: function() {
			this.canvasDIV.innerHTML = this.thtm;
			this.thtm = "";
		},
		
		Clear: function() {
			this.canvas.html("");
			this.thtm = "";
		},
		
		DrawLine: function(x1, y1, x2, y2, zorder) {
				
			
			if(x1 > x2)
			{
				var _x2 = x2;
				var _y2 = y2;
				x2 = x1;
				y2 = y1;
				x1 = _x2;
				y1 = _y2;
			}
			var dx = x2-x1, dy = Math.abs(y2-y1),
			x = x1, y = y1,
			yIncr = (y1 > y2)? -1 : 1;

			var s = this.stroke;
			if(dx >= dy)
			{
				if(dx > 0 && s-3 > 0)
				{
					var _s = (s*dx*Math.sqrt(1+dy*dy/(dx*dx))-dx-(s>>1)*dy) / dx;
					_s = (!(s-4)? Math.ceil(_s) : Math.round(_s)) + 1;
				}
				else var _s = s;
				var ad = Math.ceil(s/2);

				var pr = dy<<1,
				pru = pr - (dx<<1),
				p = pr-dx,
				ox = x;
				while(dx > 0)
				{--dx;
					++x;
					if(p > 0)
					{
						this._MakeDiv(ox, y, x-ox+ad, _s, zorder);
						y += yIncr;
						p += pru;
						ox = x;
					}
					else p += pr;
				}
				this._MakeDiv(ox, y, x2-ox+ad+1, _s, zorder);
			}

			else
			{
				if(s-3 > 0)
				{
					var _s = (s*dy*Math.sqrt(1+dx*dx/(dy*dy))-(s>>1)*dx-dy) / dy;
					_s = (!(s-4)? Math.ceil(_s) : Math.round(_s)) + 1;
				}
				else var _s = s;
				var ad = Math.round(s/2);

				var pr = dx<<1,
				pru = pr - (dy<<1),
				p = pr-dy,
				oy = y;
				if(y2 <= y1)
				{
					++ad;
					while(dy > 0)
					{--dy;
						if(p > 0)
						{
							this._MakeDiv(x++, y, _s, oy-y+ad, zorder);
							y += yIncr;
							p += pru;
							oy = y;
						}
						else
						{
							y += yIncr;
							p += pr;
						}
					}
					this._MakeDiv(x2, y2, _s, oy-y2+ad, zorder);
				}
				else
				{
					while(dy > 0)
					{--dy;
						y += yIncr;
						if(p > 0)
						{
							this._MakeDiv(x++, oy, _s, y-oy+ad, zorder);
							p += pru;
							oy = y;
						}
						else p += pr;
					}
					this._MakeDiv(x2, oy, _s, y2-oy+ad+1, zorder);
				}
			}

		}
	};
	
	
	this.init = function() {
		this.graphics.init();
	};
}

window.PW = new PWlib();

})();