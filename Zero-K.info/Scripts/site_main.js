(function ($) {
    /**
     * returns object with url variables
     * hash parameters take precedence over search
     * parameters
     * @return {Object}
     */
    $.getUrlVars = function () {
        var loc = document.location,
            search = loc.search.replace('?', '').split('&'),
            hash = loc.hash.replace('#', '').split('&'),
            i, l, v, vars = {};

        for (i = 0, l = search.length; i < l; i++) {
            v = search[i].split('=');
            vars[v[0]] = v[1];
        }

        for (i = 0, l = hash.length; i < l; i++) {
            v = hash[i].split('=');
            vars[v[0]] = v[1];
        }

        return vars;
    }

    /**
     * returns a single value from the getUrlVars hash
     * @param {String} i The hash index
     * @return {String}
     */
    $.getUrlVar = function (i) {
        return $.getUrlVars()[i];
    };
}(jQuery));


function DynDialog(url, title) {
    $.get(url, function(data) {
        var t = $("<div></div>");
        t.html(data);
        t.appendTo(document.body);
        GlobalPageInit(t);
        t.dialog(
        {
            autoOpen: true,
            show: "fade",
            hide: "fade",
            modal: false,
            title: title,
            width: 800,
            maxHeight: 600,
            open: function () {
                setTimeout(function() {
                    $('.js_dialog').scrollTop(0);
                }, 500);
           },
            buttons: { "Close": function() { $(this).dialog("close"); } }
        });
    });
}

// Merges a query string into a URL, replacing whole keys rather than adding to them: the
// callers pass "tab=2" or a serialized form, and both mean "this is the state now".
//
// Pure, exported for tools/check-site-js.js, and separate from ReplaceHistory for the same
// reason GalaxyMapGeometry is separate from the view that draws with it - the arithmetic is
// the part worth testing, and the browser API around it is not.
//
// It replaced jQuery.param.querystring from jquery.ba-bbq.js. Two hacks went with it:
//   - stripping "#" "for IE", which URL parses correctly
//   - undoing the "[]" bbq appends to repeated keys, which nothing here adds now
function BuildHistoryUrl(currentUrl, params) {
    var url = new URL(currentUrl);
    var incoming = new URLSearchParams(String(params).replace(/^[?#]/, ""));

    // Every key the caller mentions is replaced outright, repeats included; keys it does not
    // mention are left alone. That is what merging a form's state into the address bar means.
    var mentioned = [];
    incoming.forEach(function (value, key) { if (mentioned.indexOf(key) < 0) mentioned.push(key); });
    mentioned.forEach(function (key) { url.searchParams.delete(key); });
    incoming.forEach(function (value, key) { url.searchParams.append(key, value); });

    url.hash = "";
    return url.toString();
}

function ReplaceHistory(params) {
    // window.history, not window.History: the capitalised one was History.js, a polyfill for
    // pushState on IE6-8. Every browser has had this natively since IE10.
    window.history.replaceState(params, "", BuildHistoryUrl(window.location.toString(), params));
}

/*
    A POST from script, carrying the anti-forgery token the page was rendered with.

    The rating stars used $.get, which made rating a map or a mission a GET: a crafted link set
    somebody else's rating, and [ValidateAntiForgeryToken] could not be used because there was no
    token anywhere in the request. Use this instead of $.post so the token cannot be forgotten.

    The token is read from any @Html.AntiForgeryToken() on the page - they are all equivalent for a
    given visitor and request, so the first one will do. Returns null and does NOT send when the
    page has none, because a POST that silently 400s is worse than one that never leaves: the
    caller can say so.
*/
function ZkPost(url, data, done) {
    var token = $('input[name="__RequestVerificationToken"]').first().val();
    if (!token) return null;
    var payload = $.extend({}, data || {});
    payload.__RequestVerificationToken = token;
    return $.post(url, payload, done);
}

/*
    Asks a question, puts the answer in one of the form's fields, and lets the submit go ahead.

    Use as onsubmit="return ZkPrompt(this, 'text', 'What is your slogan?', 'vote me!');".

    It replaces a pattern that did the same thing to a link's own href:

        onclick="var text = prompt(...); if (text != null) $(this).attr('href', $(this).attr('href') + '&text=' + text);"

    which had three faults. The link was a GET, so a crafted one started a poll in the visitor's
    name. The answer was pasted into a URL unescaped, so a slogan containing & or # was truncated
    or mangled. And cancelling the prompt left the navigation to happen anyway, because the handler
    returned undefined - the poll was created with no text at all.

    Returning false cancels the submit, which is the fix for the third.
*/
function ZkPrompt(form, field, question, suggestion) {
    var answer = prompt(question, suggestion);
    if (answer === null) return false;
    $(form).find('input[name="' + field + '"]').val(answer);
    return true;
}

/*
    Casts a forum vote, from the onclick of the +N / -N / cancel controls.

    They were <a href> - GETs - so a crafted link voted on the visitor's behalf, and they could not
    become forms: the vote widget renders inside PostList's Ajax.BeginForm, which is a real filter
    form with its own inputs, and a form cannot nest inside another.

    The server answers "" when the vote was taken and a sentence when it was refused - banned from
    the forum, too low a level, not enough karma - so an empty reply means reload to show the new
    count, and anything else is a message for the voter. That is why VotePost stopped redirecting:
    a redirect tells an XHR nothing it can act on.
*/
function ZkVote(url) {
    var sent = ZkPost(url, {}, function (reply) {
        if (reply) alert(reply);
        else window.location.reload();
    });
    if (!sent) alert("Cannot vote: this page carries no anti-forgery token.");
    return false;
}

if (typeof module !== "undefined" && module.exports) module.exports = { BuildHistoryUrl: BuildHistoryUrl, ZkPost: ZkPost, ZkPrompt: ZkPrompt, ZkVote: ZkVote };

/*
    Tells the visitor's own game client to act on a command.

    This used a bare $.ajax, and $.ajax with no `type` is a GET - so /Lobby/SendCommand accepted
    one, and any page the visitor loaded could drive their lobby client. The action is [HttpPost]
    with a token now, which this has to match.
*/
function SendLobbyCommand(link) {
    var sent = ZkPost("/Lobby/SendCommand", { link: link }, function (data) {
        if (data != null && data.length > 0) alert(data);
    });
    if (!sent) {
        alert("Cannot send to lobby: this page carries no anti-forgery token.");
        return;
    }
    sent.fail(function () {
        alert("Error sending the command to lobby, please try again later");
    });
}

function CopyToClipboard(elem) {
    // create hidden text element, if it doesn't already exist
    var targetId = "_hiddenCopyText_";
    var isInput = elem.tagName === "INPUT" || elem.tagName === "TEXTAREA";
    var isHref = elem.tagName === "A";
    var origSelectionStart, origSelectionEnd;
    var target;
    if (isInput) {
        // can just use the original source element for the selection and copy
        target = elem;
        origSelectionStart = elem.selectionStart;
        origSelectionEnd = elem.selectionEnd;
    } else if (isHref) {
        target = document.getElementById(targetId);
        if (!target) {
            target = document.createElement("textarea");
            target.style.position = "absolute";
            target.style.left = "-9999px";
            target.style.top = "0";
            target.id = targetId;
            document.body.appendChild(target);
        }
        target.textContent = elem.getAttribute("href");
    } else {
        // must use a temporary form element for the selection and copy
        target = document.getElementById(targetId);
        if (!target) {
            target = document.createElement("textarea");
            target.style.position = "absolute";
            target.style.left = "-9999px";
            target.style.top = "0";
            target.id = targetId;
            document.body.appendChild(target);
        }
        target.textContent = elem.textContent;
    }
    // select the content
    var currentFocus = document.activeElement;
    target.focus();
    target.setSelectionRange(0, target.value.length);

    // copy the selection
    var succeed;
    try {
        succeed = document.execCommand("copy");
    } catch (e) {
        succeed = false;
    }
    // restore original focus
    if (currentFocus && typeof currentFocus.focus === "function") {
        currentFocus.focus();
    }

    if (isInput) {
        // restore prior selection
        elem.setSelectionRange(origSelectionStart, origSelectionEnd);
    } else {
        // clear temporary content
        target.textContent = "";
    }
    return succeed;
}

var isBusy = false;
var ajaxScrollCount = 40;
var ajaxScrollOffset = 40;
var ajaxScrollEnabled = true;


function GlobalPageInit(root) {
    var s = root;
    if (s == null) s = $(document);

    s.find(".js_tabs").tabs({
        selected: parseInt($.getUrlVars().tab),
        ajaxOptions: {
            success: function(xhr, status, index, anchor) {
                $(document).find(".js_tabs").each(function() {
                    GlobalPageInit($(this));
                    ReplaceHistory("tab=" + $(this).tabs("option", "selected"));
                });
            }
        }
    });
    /*
    s.find(".js_tabs").each(function () {
        var tabval = $.getUrlVars().tab;
        if (tabval) {
            $(this).tabs("option", "selected",parseInt(tabval));

         //   $(this).tabs("load", parseInt($.getUrlVars().tab));
        }
    });*/


    s.find(".js_confirm").click(function() {
        var answer = confirm("Do you really want to do it?");
        return answer;
    });

    // Same idea, with the question spelled out. PostLink's confirmMessage puts the text in an
    // attribute rather than building an onclick="return confirm('...')" by string concatenation,
    // which is what the links this replaced did - so a quote in an interpolated planet name can
    // no longer end the JavaScript string early.
    s.find("[data-confirm]").click(function() {
        return confirm($(this).attr("data-confirm"));
    });

    s.find(".js_dialog").dialog(
        {
            autoOpen: true,
            show: "fade",
            hide: "fade",
            modal: false,
            width: 800,
            maxHeight: 600,
            open: function() {
                setTimeout(function () {
                    $('.js_dialog').scrollTop(0);
                }, 500);
            },
            buttons: { "Close": function() {$(this).dialog("close"); } }
        }
    );

    s.find(".js_datepicker").datepicker($.datepicker.regional["en"]);
    s.find(".js_datetimepicker").datetimepicker();

    // buttonification
    s.find(":submit").not(".postlink-button").button();
    s.find(":button").not(".postlink-button").button();
    s.find(".js_button").button();
    s.find(".js_accordion").accordion();

    // selection for gird
    s.find(".js_selectrow").click(function() {
        var tr = $(this).closest("tr");
        if (tr.hasClass("row_selected"))
            tr.removeClass("row_selected");
        else
            tr.addClass("row_selected");
    });

    /* ajax form updater and scorll based loader
    It updates form on submit using ajax - sending offset 0 to it when user clicks

    It also updates form when user scrolls to bottom - sending current offset to it, this continues until controller returns data.

    busy element is made visible to display loading progress
    */
    var frm = s.find("#ajaxScrollForm");
    var prg = s.find("#busy");
    var target = s.find("#ajaxScrollTarget");

    if (frm && typeof frm.attr("id") != "undefined") {
        window.onscroll = function() {
            if (!ajaxScrollEnabled) {
                return;
            }
            var el = document.documentElement;
            var page = $("body");
            // bugfix for ebkit based stuff 
            // chrome needs scrolLTop out of jquery by the page. Object Opera, IE, Firefox take the original dom property
            //var scrollTop = el.scrollTop;
            //if (scrollTop == null || scrollTop == 0)
            var scrollTop = page.scrollTop();
            if (scrollTop == 0) scrollTop = el.scrollTop;
            if (el.scrollHeight - (scrollTop + el.clientHeight) < 50) {
                ajaxScrollEnabled = false;
                prg.show();
                $.post(frm.attr("action"), frm.serialize() + "&offset=" + ajaxScrollOffset, function(ret) {
                    target.append(ret);
                    ajaxScrollOffset = ajaxScrollOffset + ajaxScrollCount;
                    if (ret == "") ajaxScrollEnabled = false;
                    else ajaxScrollEnabled = true;
                    prg.hide();
                });
            }
        };

        frm.submit(function() {
            ajaxScrollEnabled = false;
            ajaxScrollOffset = 0;
            prg.show();
            $.post(frm.attr("action"), frm.serialize() + "&offset=" + ajaxScrollOffset, function(ret) {
                target.html(ret);
                ajaxScrollOffset = ajaxScrollCount;
                ajaxScrollEnabled = true;
                prg.hide();
                ReplaceHistory(frm.serialize());

            });
            return false;
        });
    }


    // img zoomer
    s.find("img.zoom").each(function() {
        $.data(this, "size", { width: $(this).width(), height: $(this).height() });
    }).hover(function() {
        $(this).stop().animate({ height: $.data(this, "size").height * 4, width: $.data(this, "size").width * 4 }, 300);
    }, function() {
        $(this).stop().animate({ height: $.data(this, "size").height, width: $.data(this, "size").width }, 600);
    });

    s.find(".js_expand").toggler({ speed: 0 });

    s.find(".js_clipboard").click(function() {
        CopyToClipboard($(this).get(0));
        alert("Copied to clipboard, use ctrl+v to paste");
        return false;
    });


    s.find("[data-preview]").each(function (i, trigger) {
        var name = $(trigger).data("preview");
        var txtSource = "[name='" + name + "']";
        $(trigger).click(function() {
            $.post("/Forum/Preview", {
                text: $(txtSource).val()
            }, function(data) {
                var dialogDiv = $("<div></div>");
                dialogDiv.html(data);
                dialogDiv.appendTo(document.body);
                GlobalPageInit(dialogDiv);
                dialogDiv.dialog(
                {
                    autoOpen: true,
                    show: "fade",
                    hide: "fade",
                    modal: false,
                    title: "Preview (close before opening a new one)",
                    width: 800,
                    naxHeight : 600,
                    open: function() {
                        $(trigger).hide();
                        var refresh = function () {
                            $.post("/Forum/Preview", {
                                text: $(txtSource).val()
                            },
                                function(d2) {
                                    dialogDiv.html(d2);
                                    GlobalPageInit(dialogDiv);
                                });
                            //if (!$(trigger).is(":visible")) window.setTimeout(refresh, 2000);
                        };
                        setTimeout(function () {
                            $('.js_dialog').scrollTop(0);
                        }, 500);
                        //window.setTimeout(refresh, 2000);
                    },
                    close: function() {
                        dialogDiv.detach();
                        $(trigger).show();
                    },
                    buttons: {
                        "Close": function() {
                            $(this).dialog("close");
                        }
                    }
                });

                

                event.preventDefault();
            });
        });
    });

    s.find("[data-autocomplete]").each(function(i, el) {
        var url = $(el).data("autocomplete");
        var action = $(el).data("autocomplete-action");

        $(el).autocomplete({
            minLength: 1,
            delay: 0,
            source: url,
            select: function (event, ui) {
                if (action === "submit") {
                    $(el).closest("form").submit();
                } else if (action === "goto") {
                    document.location = ui.item.url;
                } else if (action === "add") {
                    var name = $(el)[0].id;
                    var form = $(el).closest("form");
                    $('<input>').attr({
                        type: 'hidden',
                        name: name + '',
                        id: name + 'userinput' + ui.item.id,
                        value: ui.item.id
                    }).appendTo(form);
                    //$('<span class="ui-autocomplete">' + ui.item.value + '</span>').appendTo(form);
                    removeButton = $(" <a ><img src='/img/delete_trashcan.png' class='icon16' /></a><br />");
                    userDisplay = $("<span></span>").data("item.autocomplete", ui.item).append($("<a></a> ").html(ui.item.label)).append(removeButton).appendTo($(form).find("#" + name + "players"));
                    userDisplay.attr({ id: name + 'userdisp' + ui.item.id });
                    $(removeButton).click(function () {
                        $(form).find("#" + name + "userinput" + ui.item.id).remove();
                        $(form).find("#" + name + "userdisp" + ui.item.id).remove();
                    });
                }
            }
        }).data('ui-autocomplete')._renderItem = function (ul, item) {
            item.label = item.label.replace("a href", "a hack");
            return $("<li></li>").data("item.autocomplete", item).append($("<a></a>").html(item.label)).appendTo(ul);
        };
    });

    SetupGrid(s);


    $(".qtip").remove(); // remove all floating tooltips - breaks autorefresh with init
}


// setup busy indicator
$(function() {
    $(document).ajaxStart(function () {
        isBusy = true;
        setTimeout("if (isBusy) $('#busy').show('fade');", 500);
    });
    $(document).ajaxStop(function () {
        isBusy = false;
        $("#busy").hide();
    });
})