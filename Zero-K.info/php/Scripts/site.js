var ajaxScrollCount = 40;
var ajaxScrollOffset = 40;
var ajaxScrollEnabled = true; 
/* ajax form updater and scorll based loader
It updates form on submit using ajax - sending offset 0 to it when user clicks

It also updates form when user scrolls to bottom - sending current offset to it, this continues until controller returns data.

AjaxScrollProgress element is made visible to display loading progress
*/

$(document).ready(function () {
  $('a.delete').click(function () {
    var answer = confirm('Really delete?');
    return answer;
  });


  var frm = $("#ajaxScrollForm");
  var prg = $("#ajaxScrollProgress");
  var target = $("#ajaxScrollTarget");
  if (frm) {
    window.onscroll = function () {
      if (!ajaxScrollEnabled) return;
      var el = document.documentElement;
      if (el.scrollHeight - (document.documentElement.scrollTop + document.documentElement.clientHeight) < 50) {
        ajaxScrollEnabled = false;
        prg.show();
        $.post(frm.attr("action"), frm.serialize() + "&offset=" + ajaxScrollOffset, function (ret) {
          target.append(ret);
          ajaxScrollOffset = ajaxScrollOffset + ajaxScrollCount;
          if (ret == '') ajaxScrollEnabled = false;
          else ajaxScrollEnabled = true;
          prg.hide();
        });
      }
    };
  }

  frm.submit(function () {
    ajaxScrollEnabled = false;
    ajaxScrollOffset = 0;
    prg.show();
    $.post(frm.attr("action"), frm.serialize() + "&offset=" + ajaxScrollOffset, function (ret) {
      target.html(ret);
      ajaxScrollOffset = ajaxScrollCount;
      ajaxScrollEnabled = true;
      prg.hide();
    });
    return false;
  });

});

