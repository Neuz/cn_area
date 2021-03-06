# cn_area

国家统计局-全国统计用区划代码和城乡划分代码


---

## 内容列表

- [cn_area](#cn_area)
  - [内容列表](#内容列表)
  - [数据结构说明](#数据结构说明)
  - [更新日志](#更新日志)
  - [许可](#许可)

## 数据结构说明

```
CREATE TABLE "t_area_base" (
  "FId" CHARACTER(36) NOT NULL,  -- 主键
  "FPId" CHARACTER(36) NOT NULL, -- 父级主键
  "FGrade" NVARCHAR(255),        -- 等级；Province/City/County/Town/Village
  "FPGrade" NVARCHAR(255),       -- 父级等级
  "FCode" NVARCHAR(255),         -- 代码
  "FName" NVARCHAR(255),         -- 名称
  "FChildUrl" NVARCHAR(255),     -- 子级URL
  PRIMARY KEY ("FId")
);
```

## 更新日志

| 更新时间 | 数据来源 | 数据类型 | 下载 | 大小 |
| --- | --- | --- | --- | --- |
| 2020年 | [国家统计局](http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2020/index.html) | SQLite3 | [data.7z](https://github.com/Neuz/cn_area/releases/download/2020/data.7z) |  [![](https://img.shields.io/badge/size-26.9%20MB-brightgreen)](https://github.com/Neuz/cn_area/releases/download/2020/data.7z) 



## 许可

[![GitHub](https://img.shields.io/github/license/neuz/cn_area?style=flat-square)](LICENSE)




