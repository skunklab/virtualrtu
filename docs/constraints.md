Modbus Constraints for Virtual RTU
==================================

1.0 Overview
============

There are conditions where the owner of the RTU may require Modbus clients to be constrained in their operations. For example, constraining how a client can
interact with a control system that uses an RTU. Therefore, we have implemented
JSON configurable constraints for the VRTU to enable these scenarios.

1.1 Notational Conventions
==========================

The keywords “MUST”, “MUST NOT”, “REQUIRED”, “SHALL”, “SHALL NOT”, “SHOULD”, “SHOULD NOT”, “RECOMMENDED”, “MAY”, and “OPTIONAL” in this document are to be interpreted as described in [RFC 2119]. This specification uses the following syntax within normative outlines:

Characters are appended to indicate cardinality:

“?” (0 or 1)

“\*” (0 or more)

“+” (1 or more)

The character “\|” is used to indicate a choice between alternatives.

The characters “(” and “)” are used to indicate that contained items are to be
treated as a group with respect to cardinality or choice.

The characters “[” and “]” are used to call out references and property names.

1.2 Glossary
------------

**Constraint** – A rule that identifies specific coils or registers in that can
be allowed or denied access within a scope. A constraint may be any of the
following types (AllowAll \| DenyAll \| Filter).

**Scope** – A Modbus function type. One of (1-6),8,11,12,15,16. See [Table-1].

**Filter** – A range of Modbus address that may be allowed or denied access.

**Permission** – One of allow or deny access.

1.3 Examples
============

The following examples show how constraints can be applied to Modbus function
type “1”, i.e., read coils.

### 1.3.1 Example Configuration of Single Device with Range restriction on Read Coils
```
{ "deviceId": "device1", "virtualRtuId": "testvrtu", "storageConnectionString": null, "iotHubConnectionString": null, "base64Template": null, "expiryMinutes": 525600, "module": { "moduleId": "fieldgateway", "loggingLevel": "Debug", "instrumentationKey": "c6e42c8d-054b-4097-93fd-09a4734bdfac", "slaves": [ { "unitId": 1, "ipAddress": "192.168.0.1", "port": 502, "alias": 0, "constraint": "Filter" "scope": 1 "filters": [ "permission": "Allow" "start": 1 "end": 23 ] } ] } } |
```


### 1.3.2 Example – Allow All Coils Constraint

A constraint that allows Modbus function \#1 to read any coil.
```
 { “constraint”: “AllowAll” “scope”: 1 }
```

### 1.3.3 Example – Deny All Coils Constraint

A constraint that denies Modbus function \#1 to read any coil..
```
{ “constraint”: “DenyAll” “scope”: 1 }
```

### 1.3.4 Example – Read Coils Filter Constraint (Allow)

A constraint that applies a filter that allows Modbus function \#1 – Read Coils
to only read in the range 1-23, i.e., 0-22 in the Modbus message.
```
{ “constraint”: “Filter” “scope”: 1 “filters”: [ “permission”: “Allow” “start”: 1 “end”: 23 ] } |
```


### 1.3.5 Example – Read Coils Multiple Filter Constraint (Deny)

A constraint that applies a filter that allows Modbus function \#1 – Deny coils
to be read in the range 1-23, i.e., 0-22 in the Modbus message.
```
{ “constraint”: “Filter” “scope”: 1 “filters: [ “permission”: “Deny” “start”: 1 “end”: 23 ] } 
```

2.0 Properties 
===============

2.1 Constraints & Scopes
------------------------

A *scope* references to the Modbus function type, defined in [Table 1], which
applies the *constraint* to the specific function type. The *scope* element is
REQUIRED when applying a *constraint*; otherwise the JSON is an invalid format.
If a *constraint* for a *scope* is undefined, then is it the equivalent is
allowing all messages for the undefined *scope*.

The *constraint* element defines the type of constraint applied to the *scope*,
which is one of the following values:

-   AllowAll

-   DenyAll

-   Filter

AllowAll indicates that all interactions within the *scope* are available.
DenyAll indicates that no interaction is available for the *scope*. Filter
indicates that one or more range constraints will be applied to the *scope*.
[Table 1] indicates which scope can have a Filter *constraint*.

2.2 Filter
----------

A *filter* is a range constraint that indicates a range of coils or registers
that are either allowed or denied to Modbus clients. A *filter* encodes a
*permission* element with the value of “Allow” or “Deny”, indicating whether the
Modbus client is allowed or denied access to the encoded range. The *filter*
element also encodes a *start* and *end* element. The *start* element is the
lowest order range, and the *end* element is the highest order range applied by
the *filter*.

Table 1
=======

| Function | Description                      | Constraints               | Start Min |
|----------|----------------------------------|---------------------------|-----------|
| 1        | Read Coil                        | AllowAll, DenyAll, Filter | 1         |
| 2        | Read Discrete Input              | AllowAll, DenyAll, Filter | 10001     |
| 3        | Read Holding Registers           | AllowAll, DenyAll, Filter | 40001     |
| 4        | Read Input Registers             | AllowAll, DenyAll, Filter | 30001     |
| 5        | Write Single Coil                | AllowAll, DenyAll, Filter |           |
| 6        | Write Single Holding Register    | AllowAll, DenyAll, Filter |           |
| 8        | Diagnostics                      | AllowAll, DenyAll         |           |
| 11       | Fetch Event Count                | AllowAll, DenyAll         |           |
| 12       | Fetch Event Log                  | AllowAll, DenyAll         |           |
| 15       | Write Multiple Coils             | AllowAll, DenyAll, Filter | 1         |
| 16       | Write Multiple Holding Registers | AllowAll, DenyAll, Filter | 40001     |
