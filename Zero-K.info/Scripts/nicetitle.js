// adds hover tooltip for "title" atributes on elements a, div, span, th and input. Its styled using nicetitle css class
// if tooltip text starts with "$", it gets tooltip asynchronously from AJAX  - calls Home.mvc/GetTooltip(key)  
// if url in hyperlink contains dialog_mode=1, creates windows_open command for it


//What is this for?
//window.setInterval(makeNiceTitles, 1000);

var niceTitlesJS = new Object();

niceTitlesJS.makeNiceTitles = function () {
    $("[title]").each(
        function (index) {

            var nicetitle = $(this).attr("title");
            
            var qtipOptions = 
            { position: 
                { viewport: $(window),
                  my: 'left top',
                  at: 'right top',
                  adjust: 
                  { method: 'flip shift'
                  }
                },
                style:
                { classes: 'nicetitle'
                },
                show:
                { solo: $(document)
                }
            }

            var asyncMode = nicetitle.charAt(0) == '$';
            if (asyncMode)
            { qtipOptions['content'] = 
              { text: "loading...<img src='/img/Loader.gif'>",
                ajax:
                { url: '/Home/GetTooltip',
                  type: "GET",
                  data: {key: nicetitle}
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

$().ready(niceTitlesJS.makeNiceTitles);