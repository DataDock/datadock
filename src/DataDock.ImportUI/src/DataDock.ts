import * as _ from "lodash";
import moment from "moment";

export class Helper {
  public static slugify(original: string) {
    return _.camelCase(_.deburr(_.trim(original)));
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
