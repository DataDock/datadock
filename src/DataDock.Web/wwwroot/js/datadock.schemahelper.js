function getMetadataDatasetId(ifNotFound) {
    if (templateMetadata) {
        var templatePrefix = templateMetadata["url"];
        if (templatePrefix) {
            return templatePrefix;
        }
    }
    return ifNotFound;
}

function getMetadataIdentifier(aboutUrlPrefix, ifNotFound) {
    if (templateMetadata) {
        var templateIdentifier = templateMetadata["aboutUrl"];
        var templatePrefix = templateMetadata["url"];
        if (templateIdentifier && templatePrefix) {
            var templateAboutUrl = templatePrefix.replace("id/dataset/", "id/resource/");
            // swap template prefix to current prefix
            var identifier = templateIdentifier.replace(templateAboutUrl, aboutUrlPrefix);
            return identifier;
        }
    }
    return ifNotFound;
}

function getMetadataIdentifierColumnName() {
    if (templateMetadata) {
        var templateIdentifier = templateMetadata["aboutUrl"];
        if (templateIdentifier) {
            try {
                // get chars between { and }
                var col = templateIdentifier.substring(templateIdentifier.lastIndexOf("{") + 1, templateIdentifier.lastIndexOf("}"));
                if (col !== "_row") {
                    // ignore _row as that is not a col
                    return col;
                }
            } catch (error) {
                return "";
            }
        }
    }
    // return blank for errors or _row, as it will fall back to _row
    return "";
}

function getMetadataTitle(ifNotFound) {
    if (templateMetadata) {
        var title = templateMetadata["dc:title"];
        return title;
    }
    return ifNotFound;
}

function getMetadataDescription() {
    if (templateMetadata) {
        var desc = templateMetadata["dc:description"];
        return desc;
    }
    return "";
}

function getMetadataLicenseUri() {
    if (templateMetadata) {
        var licenseUri = templateMetadata["dc:license"];
        return licenseUri;
    }
    return "";
}

function getMetadataTags() {
    if (templateMetadata) {
        var tags = templateMetadata["dcat:keyword"];
        return tags.join();
    }
    return "";
}

function getMetadataColumnTemplate(columnName) {
    if (templateMetadata) {
        var tableSchema = templateMetadata["tableSchema"];
        if (tableSchema) {
            var metadataColumns = tableSchema["columns"]; // array
            if (metadataColumns) {
                for (let i = 0; i < metadataColumns.length; i++) {
                    if (metadataColumns[i].name === columnName) {
                        return metadataColumns[i];
                    }
                }
            }
        }
    }
    return {};
}

function getColumnTitle(template, ifNotFound) {
    if (template) {
        var titles = template["titles"];
        if (titles) {
            // first item of array
            return titles[0];
        }
    }
    return ifNotFound;
}

function getColumnPropertyUrl(template, ifNotFound) {
    if (template) {
        var propUrl = template["propertyUrl"];
        if (propUrl) {
            return propUrl;
        }
    }
    return ifNotFound;
}

function getColumnDatatype(template) {
    if (template) {
        return template["datatype"];
    }
    return "";
}

function getColumnSuppressed(template) {
    if (template) {
        return template["suppressOutput"];
    }
    return false;
}