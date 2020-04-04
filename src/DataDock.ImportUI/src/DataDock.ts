import * as _ from "lodash";
import moment from "moment";

export class Helper {
  public static slugify(original: string) {
    return _.camelCase(_.deburr(_.trim(original)));
  }

  public static getColumnWithValueUrl(
    columnSchemas: any[],
    valueUrl: string
  ): any {
    return _.findIndex(columnSchemas, (columnSchema: any) => {
      return "valueUrl" in columnSchema && columnSchema.valueUrl === valueUrl;
    });
  }

  public static getColumnWithName(
    columnSchemas: any[],
    columnName: string
  ): any {
    return _.findIndex(columnSchemas, (columnSchema: any) => {
      return "name" in columnSchema && columnSchema.name === columnName;
    });
  }

  public static pullUpMeasureColumns(templateMetadata: any) {
    let columns: any[] = templateMetadata.tableSchema.columns;
    for (let i = 0; i < columns.length; i++) {
      let c = columns[i];
      if (c.virtual) break; // Measure columns must be original source CSV columns
      if (c.columnType === "measure") {
        let resourceColIx = this.getColumnWithValueUrl(columns, c.aboutUrl);
        if (resourceColIx > 0) {
          c.measure = columns[resourceColIx];
          columns.splice(resourceColIx, 1);
        }
        if (Schema.facetColumn in c) {
          c.facets = [];
          c[Schema.facetColumn].forEach((facetName: string) => {
            let facetColumnIx = this.getColumnWithName(columns, facetName);
            if (facetColumnIx > 0) {
              c.facets.push(columns[facetColumnIx]);
              columns.splice(facetColumnIx, 1);
            }
          });
        }
      }
    }
  }

  public static pushDownMeasureColumns(templateViewModel: any) {
    let columns: any[] = templateViewModel.tableSchema.columns;
    columns.forEach((c: any, i: number) => {
      if (c.columnType == "measure") {
        // Push down the measure column, recording its valueUrl as this columns aboutUrl
        c.aboutUrl = c.measure.valueUrl;
        columns.push(c.measure);
        delete c.measure;
        if (c.facets) {
          // Push down each facet column, recording their names in the facetColumn property of this column
          c[Schema.facetColumn] = [];
          c.facets.forEach((facet: any) => {
            c[Schema.facetColumn].push(facet.name);
            columns.push(facet);
          });
          delete c.facets;
        }
      }
    });
  }

  public static makeTemplateViewModel(templateMetata: any): any {
    let templateViewModel = _.cloneDeep(templateMetata);
    let facetColumns: string[] = [];
    let measureResources: string[] = [];
    templateViewModel.tableSchema.columns.forEach((columnSchema: any) => {
      // Add uri/uriTemplate datatype when the valueUrl property is present
      if ("valueUrl" in columnSchema) {
        if (
          columnSchema.valueUrl.startsWith("{") &&
          columnSchema.valueUrl.endsWith("}")
        ) {
          columnSchema.datatype = "uri";
        } else {
          columnSchema.datatype = "uriTemplate";
        }
      }
      // Add a columnType property
      if (columnSchema.suppressOutput) {
        columnSchema.columnType = "suppressed";
      } else if (columnSchema[Schema.columnType]) {
        columnSchema.columnType = columnSchema[Schema.columnType];
        if (columnSchema.columnType == "measure") {
          measureResources.push(columnSchema.aboutUrl);
          if (Schema.facetColumn in columnSchema) {
            columnSchema[Schema.facetColumn].forEach((fc: any) =>
              facetColumns.push(fc)
            );
          }
        }
      } else {
        columnSchema.columnType = "standard";
      }

      if (columnSchema.virtual) {
        if (
          "valueUrl" in columnSchema &&
          measureResources.indexOf(columnSchema.valueUrl) >= 0
        ) {
          // Hide the virutal column used as the parent for a measure column
          columnSchema.hidden = true;
        }
        if (facetColumns.indexOf(columnSchema.name) >= 0) {
          // Hide the virual columns used to define facets of a measure column
          columnSchema.hidden = true;
        }
      }
    });
    this.pullUpMeasureColumns(templateViewModel);
    return templateViewModel;
  }

  public static makeTemplate(templateViewModel: any): any {
    let templateMetadata = _.cloneDeep(templateViewModel);
    this.pushDownMeasureColumns(templateMetadata);
    templateMetadata.tableSchema.columns.forEach((columnSchema: any) => {
      // Convert columnType to suppressOutput or dd:columnType extension property
      if (columnSchema.columnType == "suppressed") {
        columnSchema.suppressOutput = true;
      } else if (columnSchema.columnType == "standard") {
        delete columnSchema.suppressOutput;
        delete columnSchema[Schema.columnType];
      } else {
        delete columnSchema.suppressOutput;
        columnSchema[Schema.columnType] = columnSchema.columnType;
      }
      delete columnSchema.columnType;

      if (columnSchema.datatype == "uri" && !columnSchema.virtual) {
        // Replace uri datatype with a valueUrl property
        columnSchema.valueUrl = "{" + columnSchema.name + "}";
        delete columnSchema.datatype;
      }
      if (columnSchema.datatype == "uriTemplate") {
        // Remove the datatype property
        delete columnSchema.datatype;
      }

      // Remove hidden property
      delete columnSchema.hidden;
    });
    return templateMetadata;
  }
}

const DATE_FORMATS = [
  "MM/DD/YYYY",
  "MM/DD/YY",
  "M/D/YYYY",
  "M/D/YY",
  "DD/MM/YYYY",
  "DD/MM/YY",
  "D/M/YYYY",
  "D/M/YY",
  "Do MMMM YYYY",
  "Do MMMM YY",
  "Do MMM YYYY",
  "Do MMM YY",
  "YYYY-MM-DD",
  "YYYY-MM-DDZ",
  "YYYY-MM-DDZZ",
  "-YYYY-MM-DD",
  "-YYYY-MM-DDZ",
  "-YYYY-MM-DDZZ"
];

const DATETIME_FORMATS = [
  "YYYY-MM-DDTHH:mm:ss",
  "YYYY-MM-DDTHH:mm:ss.S",
  "YYYY-MM-DDTHH:mm:ss.SS",
  "YYYY-MM-DDTHH:mm:ss.SSS",
  "YYYY-MM-DDTHH:mm:ss.SSSS",
  "YYYY-MM-DDTHH:mm:ssZ",
  "YYYY-MM-DDTHH:mm:ss.SZ",
  "YYYY-MM-DDTHH:mm:ss.SSZ",
  "YYYY-MM-DDTHH:mm:ss.SSSZ",
  "-YYYY-MM-DDTHH:mm:ss",
  "-YYYY-MM-DDTHH:mm:ss.S",
  "-YYYY-MM-DDTHH:mm:ss.SS",
  "-YYYY-MM-DDTHH:mm:ss.SSS",
  "-YYYY-MM-DDTHH:mm:ss.SSSS",
  "-YYYY-MM-DDTHH:mm:ssZ",
  "-YYYY-MM-DDTHH:mm:ss.SZ",
  "-YYYY-MM-DDTHH:mm:ss.SSZ",
  "-YYYY-MM-DDTHH:mm:ss.SSSZ"
];

export class SnifferOptions {
  public skipHeader: boolean | number = true;
  public isUri = (x: string) => {
    return /^(((https?)|ftp):\/\/.+)$/.test(x);
  };
  public isInteger = (x: string) => {
    return /^((\+|-)?\d+)$/.test(x);
  };
  public isDecimal = (x: string) => {
    return /^((\+|-)?\d+(\.\d+)?)$/.test(x);
  };
  public isFloat = (x: string) => {
    return /^(((\+|-)?\d+(\.\d+)?((E)-?\d+)?)|INF|-INF|NAN)$/i.test(x);
  };
  public isDate = (x: string) => {
    return moment(x, DATE_FORMATS, true).isValid();
  };
  public isDateTime = (x: string) => {
    return moment(x, DATETIME_FORMATS, true).isValid();
  };
  public isBoolean = (x: string) => {
    return /^(0|1|true|false|yes|no)$/i.test(x);
  };
}

export enum DatatypeEnum {
  None = 0,
  Uri = 1,
  Integer = 2,
  Decimal = 4,
  Float = 8,
  Date = 16,
  DateTime = 32,
  Boolean = 64,
  All = 127
}

export class ColumnInfo {
  public hasEmptyValues: boolean;
  public allEmptyValues: boolean;
  public datatype: DatatypeEnum;
  public sniffedType: string;

  public constructor(
    hasEmptyValues: boolean,
    allEmptyValues: boolean,
    datatype: DatatypeEnum
  ) {
    this.hasEmptyValues = hasEmptyValues;
    this.allEmptyValues = allEmptyValues;
    this.datatype = datatype;
    this.sniffedType = this.getSniffedType(datatype);
  }

  public isUri(): boolean {
    return DatatypeEnum.Uri === (this.datatype & DatatypeEnum.Uri);
  }
  public isInteger(): boolean {
    return DatatypeEnum.Integer === (this.datatype & DatatypeEnum.Integer);
  }
  public isDecimal(): boolean {
    return DatatypeEnum.Decimal === (this.datatype & DatatypeEnum.Decimal);
  }
  public isFloat(): boolean {
    return DatatypeEnum.Float === (this.datatype & DatatypeEnum.Float);
  }
  public isDate(): boolean {
    return DatatypeEnum.Date === (this.datatype & DatatypeEnum.Date);
  }
  public isDateTime(): boolean {
    return DatatypeEnum.DateTime === (this.datatype & DatatypeEnum.DateTime);
  }
  public isBoolean(): boolean {
    return DatatypeEnum.Boolean === (this.datatype & DatatypeEnum.Boolean);
  }

  protected getSniffedType(datatype: DatatypeEnum): string {
    let sniffedType = "string";
    if (datatype & DatatypeEnum.Uri) sniffedType = "uri";
    if (datatype & DatatypeEnum.Float) sniffedType = "float";
    if (datatype & DatatypeEnum.Decimal) sniffedType = "decimal";
    if (datatype & DatatypeEnum.Integer) sniffedType = "integer";
    if (datatype & DatatypeEnum.DateTime) sniffedType = "datetime";
    if (datatype & DatatypeEnum.Date) sniffedType = "date";
    if (datatype & DatatypeEnum.Boolean) sniffedType = "boolean";
    return sniffedType;
  }
}

export class DatatypeSniffer {
  private options: SnifferOptions;

  constructor(options: SnifferOptions) {
    this.options = options;
  }

  public sniffColumn(colIx: number, rows: string[][]): ColumnInfo {
    let datatype: DatatypeEnum = DatatypeEnum.All;
    let hasEmptyValues = false;
    let allEmptyValues = true;
    let skipRows = 0;
    if (typeof this.options.skipHeader === "number") {
      skipRows = this.options.skipHeader;
    } else {
      skipRows = this.options.skipHeader ? 1 : 0;
    }
    for (let i = skipRows; i < rows.length; i++) {
      if (colIx >= rows[i].length) {
        hasEmptyValues = true;
        continue;
      }
      const val = rows[i][colIx];
      if (val === "") {
        hasEmptyValues = true;
        continue;
      }

      // Non-empty value encountered
      allEmptyValues = false;

      if (datatype & DatatypeEnum.Uri && !this.options.isUri(val)) {
        datatype = datatype & ~DatatypeEnum.Uri;
      }
      if (datatype & DatatypeEnum.Integer && !this.options.isInteger(val)) {
        datatype = datatype & ~DatatypeEnum.Integer;
      }
      if (datatype & DatatypeEnum.Decimal && !this.options.isDecimal(val)) {
        datatype = datatype & ~DatatypeEnum.Decimal;
      }
      if (datatype & DatatypeEnum.Float && !this.options.isFloat(val)) {
        datatype = datatype & ~DatatypeEnum.Float;
      }
      if (datatype & DatatypeEnum.Date && !this.options.isDate(val)) {
        datatype = datatype & ~DatatypeEnum.Date;
      }
      if (datatype & DatatypeEnum.DateTime && !this.options.isDateTime(val)) {
        datatype = datatype & ~DatatypeEnum.DateTime;
      }
      if (datatype & DatatypeEnum.Boolean && !this.options.isBoolean(val)) {
        datatype = datatype & ~DatatypeEnum.Boolean;
      }
    }

    // Prefer Decimal over Float
    if (datatype & DatatypeEnum.Decimal && datatype & DatatypeEnum.Float) {
      datatype = datatype & ~DatatypeEnum.Float;
    }

    // Prefer Integer over Decimal
    if (datatype & DatatypeEnum.Integer && datatype & DatatypeEnum.Decimal) {
      datatype = datatype & ~DatatypeEnum.Decimal;
    }

    // Prefer Boolean over Integer
    if (datatype & DatatypeEnum.Boolean && datatype & DatatypeEnum.Integer) {
      datatype = datatype & ~DatatypeEnum.Integer;
    }

    return new ColumnInfo(hasEmptyValues, allEmptyValues, datatype);
  }

  public getDatatypes(rows: string[][]): ColumnInfo[] {
    const colCount = rows[0].length;
    let datatypeInfo: ColumnInfo[] = [];
    for (let colIx = 0; colIx < colCount; colIx++) {
      datatypeInfo.push(this.sniffColumn(colIx, rows));
    }
    return datatypeInfo;
  }
}

export class Schema {
  public static SchemaBase: string = "http://schema.datadock.io/";
  public static columnType: string = Schema.SchemaBase + "columnType";
  public static hidden: string = Schema.SchemaBase + "hidden";
  public static facetColumn: string = Schema.SchemaBase + "facetColumn";
}
