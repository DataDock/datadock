﻿
function formatResults(results, searchInput) {
    console.log(results);
    console.log(searchInput);

    var $resultsHtml = $("<div id='results-inner'></div>");
    var $hiddenDivider = $("<div/>", { 'class': "ui hidden divider" });

    $("<h2/>", { text: "Results: " + searchInput }).appendTo($resultsHtml);
    $resultsHtml.append($hiddenDivider);

    if (results && Array.isArray(results) && results.length > 0) {
        var $cards = $("<div/>", { 'class': "ui two stackable cards" }).appendTo($resultsHtml);

        $.each(results,
            function(key, ds) {
                var dsUrl = getMetadataValue(ds.csvwMetadata, "url", "");
                var lastMod = moment(ds.lastModified);
                var dsDesc = getMetadataValue(ds.csvwMetadata, "dc:description", "");
                var dsLicense = getMetadataValue(ds.csvwMetadata, "dc:license", "");
                console.log(ds);


                if (dsDesc) {
                    dsDesc = dsDesc.replace(/^(.{20}[^\s]*).*/, "$1") + "...\n";
                }

                var $card = $("<div/>",
                    {
                        'class': "card",
                        'property': "void:subset",
                        'resource': dsUrl
                    }).appendTo($cards);
                var $cardContent = $("<div/>",
                    {
                        'class': "content",
                        about: dsUrl
                    }).appendTo($card);
                var $header = $("<div/>", { 'class': "header" })
                    .append($("<h3/>",
                        {
                            'property': 'dc:title'
                        }).append($("<a />",
                            {
                                href: dsUrl,
                                text: getMetadataValue(ds.csvwMetadata, "dc:title", ds.datasetId)
                            })
                    ));
                $header.appendTo($cardContent);
                var $details = $("<dl />").appendTo($cardContent);
                // id
                $("<dt />", { text: "Identifier" }).appendTo($details);
                $("<dd />", { text: dsUrl }).appendTo($details);
                // last modified
                $("<dt />", { text: "Last Modified" }).appendTo($details);
                $("<dd />", { text: lastMod.format("D MMM YYYY") + ' at ' + lastMod.format("HH:mm") })
                    .appendTo($details);
                // id
                $("<dt />", { text: "License" }).appendTo($details);
                $("<dd />", { text: dsLicense }).appendTo($details);
                // description
                if (dsDesc !== "") {
                    $("<dt />", { text: "Description" }).appendTo($details);
                    $("<dd />", { text: dsDesc }).appendTo($details);
                }

                var $extraContent = $("<div/>",
                    {
                        'class': "extra content"
                    }).appendTo($card);
                var $span1 = $("<span/>",
                    {
                        'class': "right floated"
                    }).appendTo($extraContent);
                var $stat = $("<div/>",
                    {
                        'class': "ui mini statistic"
                    }).appendTo($span1);
                var $val = $("<div/>",
                    {
                        'class': "value",
                        'property': "void:triple",
                        'datatype': "http://www.w3.org/2001/XMLSchema#integer",
                        text: getMetadataValue(ds.voidMetadata, "void:triples", "")
                    }).appendTo($stat);
                var $label = $("<div/>",
                    {
                        'class': "label",
                        text: "Triples"
                    }).appendTo($stat);

                var downloads = getMetadataArray(ds.voidMetadata, "void:dataDump");
                if (downloads) {
                    var len = downloads.length;
                    for (var i = 0; i < len; i++) {
                        var label = "";
                        if (downloads[i].endsWith("csv")) {
                            label = "CSV";
                        } else {
                            label = "N-QUADS";
                        }
                        var $ddlink = ($("<a />",
                            {
                                href: downloads[i],
                                'class': "ui primary button mr",
                                'property': "void:dataDump",
                                text: label
                            }).appendTo($extraContent));
                        ($("<i />", { 'class': 'download icon' }).appendTo($ddlink));
                    }
                }
            });
    } else {
        //0 results
        $("<p/>", { 'class': "ui big", text: 'No datasets found.'}).appendTo($resultsHtml);
    }
    $("#results").empty().append($resultsHtml);
   

}

function getMetadataValue(metadata, propertyName, defaultValue) {
    if (metadata) {
        var value = metadata[propertyName];
        if (value) {
            return value;
        } else {
            return defaultValue;
        }
    } else {
        return null;
    }
}

function getMetadataArray(metadata, propertyName) {
    if (metadata) {
        var value = metadata[propertyName];
        if (value) {
            return value;
        } else {
            return null;
        }
    } else {
        return null;
    }
}

function find() {
    var tags = $('#tags').val();
    if (tags) {

        var query = formatQuery(tags, true);

        if (searchUri) {
            $.getJSON(searchUri + query)
                .done(function (data) {
                    //console.log(data);
                    //console.log(JSON.stringify(data));
                    $('#toc').hide();
                    if (data) {
                        $('#results').html(formatResults(data, tags));
                    } else {
                        $('#results').html('<p class="ui big">No results found.</p>');
                    }
                    
                })
                .fail(function (jqXHR, textStatus, err) {
                    console.error(err);
                    $('#datasets').text('Error: ' + err);
                });
        } else {
            //todo do not show button or allow this to run if no search URI
        }
    } else {
        //display warning
    }
}


function tagSearch(tags) {
    $('#tags').val(''); // clear search input
    var buttonId = "#" + tags;
    $(buttonId).toggleClass("loading");
    console.log(tags);
    if (tags) {
        var query = formatQuery(tags, true);

        if (searchUri) {
            $('#loader').toggleClass("active");
            $.getJSON(searchUri + query)
                .done(function (data) {
                    console.log(data);
                    console.log(JSON.stringify(data));
                    $('#toc').hide();
                    $('#results').html(formatResults(data, tags));
                    $('#loader').toggleClass("active");
                    $('#loader').hide();
                    $(buttonId).toggleClass("loading");
                })
                .fail(function (jqXHR, textStatus, err) {
                    console.error(err);
                    $('#datasets').text('Error: ' + err);
                });
        } else {
            console.error('No search URI defined.');
        }
    } else {
        //todo display warning
        console.log('No tags defined.')
    }
}

function formatQuery(tags, and) {
    var splitTags = tags.split(" ");
    var len = splitTags.length;
    var query = "";
    for (var i = 0; i < len; i++) {
        if (query !== "") {
            query = query + "&tag=" + splitTags[i];
        } else {
            query = "?tag=" + splitTags[i];
        }
    }
    if (query && len > 1 && and) {
        query = query + "&all=true";
    }
    console.log(query);
    return query;
}

function filterByTag(tag) {
    $('.card[property="void:subset"]').hide();
    $('.card[property="void:subset"]:has(span[property="dcat:keyword"]:contains("' + tag + '"))').show();
}

function clearFilter() {
    $('.card[property="void:subset"]').show();
}

function buttonSearch(tag) {
    var tagButton = $("#buttons button[data-tag=\"" + tag + "\"]");
    if (!tagButton.hasClass("basic")) {
        // remove filter
        $("#buttons button").addClass("basic");
        clearFilter();
    } else {
        // set filter
        $("#buttons button").addClass("basic");
        tagButton.removeClass("basic");
        filterByTag(tag);
    }
}

