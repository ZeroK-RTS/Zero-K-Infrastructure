// adds hover tooltip for "title" atributes on elements a, div, span, th and input. Its styled using nicetitle css class
// if tooltip text starts with "$", it gets tooltip asynchronously from AJAX  - calls Home.mvc/GetTooltip(key)  
// if url in hyperlink contains dialog_mode=1, creates windows_open command for it


//What is this for?
//window.setInterval(makeNiceTitles, 1000);

var niceTitlesJS = new Object();
niceTitlesJS.CURRENT_NICE_TITLE = null;
niceTitlesJS.cachedToolTips = new Array();

niceTitlesJS.makeNiceTitles = function () {
    var leafTitlenodes = new Array();
    $("[title]").each(
        function (index) {

            var elem = this;
            var nicetitle = $(this).attr("title");

            /*while ($(elem).parent() != $(document)
            { if ($(elem).parent().attr("title") == nicetitle)
                return;
              else
                elem = $(elem).parent()
            }*/
            
            var qtipOptions = 
            { position: 
                { viewport: $(window),
                  my: 'left top',
                  at: 'right top',
                  adjust: 
                  { method: 'flip shift',
                  },
                },
                style:
                { classes: 'nicetitle',
                },
                show:
                { solo: $(document),
                }
            }

            var asyncMode = nicetitle.charAt(0) == '$';
            if (asyncMode)
            { qtipOptions['content'] = 
              { text: "loading...<img src='/img/Loader.gif'>",
                ajax:
                { url: '/Home/GetTooltip',
                  type: "GET",
                  data: {key: nicetitle},
                }
              };
              
            }
            else
            { qtipOptions['content'] = {text: nicetitle};
            }

            $(this).qtip(qtipOptions);
        }
    );
}

niceTitlesJS.attachNiceTitleHandler = function(element) {
    var nicetitle = $(element).attr("title");
    var asyncMode = nicetitle.charAt(0) == '$';
    var niceTitleDiv = $("<div />").addClass("nicetitle");
    var niceTitleContent;
    if (!asyncMode)
        niceTitleContent = $("<span>" + nicetitle + "</span>");
    else {
        niceTitleContent = $("<span>Loading....<img src='/img/Loader.gif'></span>");
        $.get('/Home/GetTooltip?key=' + nicetitle, function (ret) {
            //cache returned data
            niceTitlesJS.cachedTooltips[nicetitle] = ret;
            // if tooltip still same, update it
            if (niceTitleDiv == CURRENT_NICE_TITLE) {
                niceTitlesJS.CURRENT_NICE_TITLE.html(ret);
            }
            //TODO: We MAY need to call update() method of jquery.tooltip to deal with the 
            //  Tooltip changing size at this point
        });
    }
    niceTitlesJS.CURRENT_NICE_TITLE = niceTitleDiv;
    return niceTitleDiv.append(niceTitleContent);
}

$().ready(niceTitlesJS.makeNiceTitles);