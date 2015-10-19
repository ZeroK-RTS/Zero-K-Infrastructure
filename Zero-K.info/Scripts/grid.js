// note this, along with unigrid class and gridhelpers.cshtml forms grids

function SetupGrid(s) {

    s.find(".js-grid-first").button(
        {
            icons: {
                primary: "ui-icon-seek-first"
            }
        });

    s.find(".js-grid-prev").button(
        {
            icons: {
                primary: "ui-icon-seek-prev"
            }
        });
    s.find(".js-grid-next").button(
        {
            icons: {
                primary: "ui-icon-seek-next"
            }
        });

    s.find(".js-grid-end").button(
        {
            icons: {
                primary: "ui-icon-seek-end"
            }
        });

    s.find(".js-disabled").button("disable");

    s.find(".js-grid-selector").each(function () {
        if ($(this).is(':checked')) {
            $(this).closest('tr').addClass('row_selected');
        }
    });

}


function getGridForm(gridID) {
    return $("#" + gridID + "page").closest('form');
}

function gridSetPage(gridID, page) {
    var pageNumberElem = $("#" + gridID + "page");
    pageNumberElem.val(page);
    var form = getGridForm(gridID);
    form.find('input').unbind();
    form.submit();
}

function gridSetSort(gridID, colName) {
    var oc = $('#' + gridID + 'order');
    var od = $('#' + gridID + 'desc');
    if (oc.val() == colName) {
        od.val(od.val() == 'True' ? 'False' : 'True');
    }
    else {
        od.val('False');
    }
    oc.val(colName);

    var form = getGridForm(gridID);
    form.find('input').unbind();
    form.submit();
}

function gridSelect(gridID, elem) {
    var checked = elem.is(':checked');
    var value = elem.val();
    gridData[gridID][value] = checked ? '1' : '0';
    var row = elem.closest('tr');
    if (checked) row.addClass('row_selected');
    else row.removeClass('row_selected');
}

function gridGetCsv(gridID) {
    var form = getGridForm(gridID);
    var url = form.attr("action") + "?" + form.serialize() + "&" + gridID + "csv=True";
    window.location = url;
}

function gridToggleRows(gridID, el) {
    var form = getGridForm(gridID);
    var checked = el.is(':checked');
    form.find('.js-grid-selector').each(function () {
        if (checked) $(this).prop('checked', true);
        else $(this).prop('checked', false);
        gridSelect(gridID, $(this));
    });
}

function gridFormHookSelections(gridID, form, name) {
    if (name == null) name = gridID + "sel";

    form.submit(function () {
        jQuery.each(gridData[gridID], function (key, val) {
            if (val == '1') {
                $("<input>").attr({
                    'type': 'hidden',
                    'name': name,
                    'value': key
                }).appendTo(form);
            }
        });
    });
}