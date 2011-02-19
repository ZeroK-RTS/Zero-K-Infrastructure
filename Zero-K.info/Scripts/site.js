var ajaxScrollCount = 40;
var ajaxScrollOffset = 40;
var ajaxScrollEnabled = true;

/* confirm dialog when class is delete */
$(document).ready(function () {
  // delete confirm dialog
  $('a.delete').click(function () {
    var answer = confirm('Really delete?');
    return answer;
  });

  /* auto submit form when data-autpostback=true */
  $(document).ready(function () {
    $("select:[autopostback=True]").change(function () {
      $(this).closest("form").submit();
    });
  });


  /* ajax form updater and scorll based loader
  It updates form on submit using ajax - sending offset 0 to it when user clicks

  It also updates form when user scrolls to bottom - sending current offset to it, this continues until controller returns data.

  AjaxScrollProgress element is made visible to display loading progress
  */
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


$(window).load(function () {
  // img zoomer
  $("img.zoom").each(function () {
    $.data(this, 'size', { width: $(this).width(), height: $(this).height() });
  }).hover(function () {
    $(this).stop().animate({ height: $.data(this, 'size').height * 4, width: $.data(this, 'size').width * 4 }, 300);
  }, function () {
    $(this).stop().animate({ height: $.data(this, 'size').height, width: $.data(this, 'size').width }, 600);
  })
});
