// adds hover tooltip for "title" atributes on elements a, div, span, th and input. Its styled using nicetitle css class
// if tooltip text starts with "$", it gets tooltip asynchronously from AJAX  - calls Home.mvc/GetTooltip(key)  
// if url in hyperlink contains dialog_mode=1, creates windows_open command for it

var niceTitlesJS = new Object();
niceTitlesJS.ajaxCache = new Object();

//Initiate the nice titles system
niceTitlesJS.initNiceTitles = function () {
  niceTitlesJS.ieFixTitleField();
  niceTitlesJS.installNiceTitleListener();
  //We are force to poll for changes in the content, as the 
  //  MVC is currently configured to use MicroSoft Ajax, not
  //  JQuery Ajax. JQuery Ajax serve us an event we could use
  //    instead of this polling
  window.setInterval(niceTitlesJS.ieFixTitleField, 1000);
}

//Make sure that IE never displays tooltips
niceTitlesJS.ieFixTitleField = function () {
  $("[title]").each(
    function (index) 
    { $(this).attr("nicetitle", $(this).attr("title"));
      $(this).removeAttr("title");
    }
  );
}

niceTitlesJS.installNiceTitleListener = function () {
  //Jquery selector for all elements with "nicetitle" attibute
  //  This works for existing and new elements, so ajax calls will be fine
  $("[nicetitle]").live("mouseenter",
    function (event) {
      var nicetitle = $(this).attr("nicetitle");
      var qtipOptions =
      { overwrite: false,
        position:
        { //Show the tooltip relative the the element that fired the event
          my: 'left top',
          at: 'right top',
          target: 'event',
          //Dont let the tool tip go off the window
          viewport: $(window),
          adjust:
          { method: 'flip shift'
          }
        },
        show:
        { //Show with no delay, "Clumsy shit of laggy"
          delay: 0,
          effect: false,
          //assign to target event "mouseover"
          event: event.type,
          //show tooltip immediately on first load
          ready: true
        },
        hide:
        { delay: 0,
          effect: false,
          event: "mouseleave"
        },
        style:
        { classes: 'nicetitle'
        }
      }
      var asyncMode = nicetitle.charAt(0) == '$';
      if (asyncMode) {
        //Process all elements with the same nicetitle attribute value
        $("[nicetitle='" + nicetitle + "']").each(
          function (index) {
            $(this).attr("nicetitle-processed", nicetitle);
            $(this).removeAttr("nicetitle");
          }
        );
        //Assign the DOM event targets for mouseenter and mouseleave
        qtipOptions.show.target = $("[nicetitle-processed='" + nicetitle + "']");
        qtipOptions.hide.target = $("[nicetitle-processed='" + nicetitle + "']");

        //Check the cache for existing Ajax Content
        if (!niceTitlesJS.ajaxCache.hasOwnProperty(nicetitle)) {
          qtipOptions['content'] =
          { text: "loading...<img src='/img/Loader.gif' style='vertical-align: middle;' width='20'>",
            ajax:
            { url: '/Home/GetTooltip',
              type: "GET",
              data: { key: nicetitle },
              success: function (data, status, jqXHR) {
                //Cache returned Ajax
                niceTitlesJS.ajaxCache[nicetitle] = data;
                //Set this tooltip content
                this.set("content.text", data);
              }
            }
          };
        }
        else {
          //Set this tooltip content to the cached Ajax data
          qtipOptions['content'] =
          { text: niceTitlesJS.ajaxCache[nicetitle]
          };
        }
      }
      else {
        //Use existing title attribute value as tooltip content
        qtipOptions['content'] = { text: nicetitle };
        $(this).attr("nicetitle-processed", $(this).attr("nicetitle"));
        $(this).removeAttr("nicetitle");
      }
      //Build tooltip onto DOM element
      $(this).qtip(qtipOptions, event);
      //Force tooltip to be shown immediately (this shouldnt be necessary, but it is)
      $(this).qtip("show", event);
    }
  );
};
$().ready(niceTitlesJS.initNiceTitles);
































































































































































