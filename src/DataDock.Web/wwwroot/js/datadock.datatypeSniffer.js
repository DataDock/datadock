var datatypeSniffer = {};
(function () {
    var Datatype = {
        Uri: 1,
        Integer: 2,
        Decimal: 4,
        Float: 8,
        Date: 16,
        DateTime: 32,
        Boolean: 64
    };

    var DATE_FORMATS = [
        'MM/DD/YYYY',
        'MM/DD/YY',
        'M/D/YYYY',
        'M/D/YY',
        'DD/MM/YYYY',
        'DD/MM/YY',
        'D/M/YYYY',
        'D/M/YY',
        'Do MMMM YYYY',
        'Do MMMM YY',
        'Do MMM YYYY',
        'Do MMM YY',
        'YYYY-MM-DD',
        'YYYY-MM-DDZ',
        'YYYY-MM-DDZZ',
        '-YYYY-MM-DD',
        '-YYYY-MM-DDZ',
        '-YYYY-MM-DDZZ',
    ];

    var DATETIME_FORMATS = [
        'YYYY-MM-DDTHH:mm:ss',
        'YYYY-MM-DDTHH:mm:ss.S',
        'YYYY-MM-DDTHH:mm:ss.SS',
        'YYYY-MM-DDTHH:mm:ss.SSS',
        'YYYY-MM-DDTHH:mm:ss.SSSS',
        'YYYY-MM-DDTHH:mm:ssZ',
        'YYYY-MM-DDTHH:mm:ss.SZ',
        'YYYY-MM-DDTHH:mm:ss.SSZ',
        'YYYY-MM-DDTHH:mm:ss.SSSZ',
        '-YYYY-MM-DDTHH:mm:ss',
        '-YYYY-MM-DDTHH:mm:ss.S',
        '-YYYY-MM-DDTHH:mm:ss.SS',
        '-YYYY-MM-DDTHH:mm:ss.SSS',
        '-YYYY-MM-DDTHH:mm:ss.SSSS',
        '-YYYY-MM-DDTHH:mm:ssZ',
        '-YYYY-MM-DDTHH:mm:ss.SZ',
        '-YYYY-MM-DDTHH:mm:ss.SSZ',
        '-YYYY-MM-DDTHH:mm:ss.SSSZ',
    ];

    var isUri = (x) => { return /^(((https?)|ftp):\/\/.+)$/.test(x); }
    var isInteger = (x) => { return /^((\+|\-)?\d+)$/.test(x); }
    var isDecimal = (x) => { return /^((\+|\-)?\d+(\.\d+)?)$/.test(x); }
    var isFloat = (x) => { return /^(((\+|\-)?\d+(\.\d+)?((E)\-?\d+)?)|INF|\-INF|NAN)$/i.test(x);}
    var isDate = (x) => { return moment(x, DATE_FORMATS, true).isValid(); }
    var isDateTime = (x) => { return moment(x, DATETIME_FORMATS, true).isValid(); }
    var isBoolean = (x) => { return /^(0|1|true|false)$/i.test(x); }


    var sniffColumn = function(rows, colIx) {
        var datatype = 127;
        var hasEmptyValues = false;
        var allEmpty = true;
        for (var i = 1; i < rows.length; i++) {
            var val = rows[i][colIx].trim();
            if (val === '') {
                hasEmptyValues = true;
                continue;
            }
            allEmpty = false;
            if (datatype & Datatype.Uri) {
                if (!isUri(val)) datatype = datatype & ~Datatype.Uri;
            }
            if (datatype & Datatype.Integer) {
                if (!isInteger(val)) datatype = datatype & ~Datatype.Integer;
            }
            if (datatype & Datatype.Decimal) {
                if (!isDecimal(val)) datatype = datatype & ~Datatype.Decimal;
            }
            if (datatype & Datatype.Float) {
                if (!isFloat(val)) datatype = datatype & ~Datatype.Float;
            }
            if (datatype & Datatype.Date) {
                if (!isDate(val)) datatype = datatype & ~Datatype.Date;
            }
            if (datatype & Datatype.DateTime) {
                if (!isDateTime(val)) datatype = datatype & ~Datatype.DateTime;
            }
            if (datatype & Datatype.Boolean) {
                if (!isBoolean(val)) datatype = datatype & ~Datatype.Boolean;
            }
        }

        var sniffedType = "string";
        if (datatype & Datatype.Uri) sniffedType = "uri";
        if (datatype & Datatype.Float) sniffedType = "float";
        if (datatype & Datatype.Decimal) sniffedType = "decimal";
        if (datatype & Datatype.Integer) sniffedType = "integer";
        if (datatype & Datatype.DateTime) sniffedType = "datetime";
        if (datatype & Datatype.Date) sniffedType = "date";
        if (datatype & Datatype.Boolean) sniffedType = "boolean";

        return {
            hasEmptyValues: hasEmptyValues,
            allEmptyValues: allEmpty,
            type: sniffedType
        }
    }

    this.getDatatypes = function (csvRows) {
        var header = csvRows[0];
        var datatypeInfo = [];
        for (var colIx = 0; colIx < header.length; colIx++) {
            var colInfo = sniffColumn(csvRows, colIx);
            datatypeInfo.push(colInfo);
        }
        return datatypeInfo;
    }


}).apply(datatypeSniffer);