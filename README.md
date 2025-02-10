

https://capnproto.org/cxx.html
https://github.com/google/flatbuffers
https://github.com/msgpack/msgpack/blob/master/spec.md


# Built-in types

P = Size for pointer,  or 8 bytes, depending on intended file size. When 8 bytes, the lowest 7 bytes at most are expected to be non zero.

A = Default alignement. Typically 1 byte for compactness, 4 or 8 bytes for machine alignment.

| Type | Base size | Code | Comment |
| :---- | :--: | -- |:---- |
| Bool | 1/8 | o | 1 bit if in an array, 1 byte if standalone |
| int8 / uint8 | 1 | b/B ||
| int16 / uint16 | 2 | w/W ||
| int32 / uint32 | 4 | i/j ||
| int64 / uint64 | 8 | l/L ||
| f64 | 4 | f | IEEE 754 |
| f64 | 8 | F | IEEE 754|
| f128 | 16 |  | standard ? |
||
| string (latin 1) | 8 | 'c' | data is a pointer to the string (possible short string optimisation). |
| string (utf-8) | 8 | 'u' | '' |
| string (utf-16) | 8 | 'U' | '' |
| string (utf-32) | 8 | 'V' | '' |
||
| struct | N | 'r'+id | Id is the struct def address (on 7 bytes if P=8). |
| enum | 1 or 2 | 'e'+id | Id is the enum def address (on 7 bytes if P=8). |
||
| ptr | P | p? | See note below |
| array | P | a? | See note below. |
| unsorted dict | P | d?? | See note below. |
| sorted dict | P | <?? | See note below. |
| hash sorted dict | P | #?? | See note below. |

String :
Possible short string optimisation, if pointer are on 8 bytes. We put size in hight order byte and use 7 remaining bytes to store small strings ("yes", "no", "today").
It's limiter to 7 chars including last null termination, is it useful ?


Pointer can be :
- pointer to am unspecified type (p_) : defines a variant value.
- pointer to a specified type (pi, pF, ..) : defines a int* or double* in C.

Array can be :
- array of unspecified item types (a_): typically a JSON array.
- array of a specified type (ai, aF, ..): defines a int[] or double[] in C

Dictionary can be :

- dict of specified key type and unspecified value type (ds_, di_, ...) : "ds_" is the typical a JSON dictionary.
- dict of specified key and value types (dss, dsi, dis, dsaf, ...)


# Built-in structures

## Variant : data of unspecified type

General rule : the type is prepended to the data

## Arrays

We store the array size, then the data using fixed size items.

It means that
- 'ai' : data is a dense array of ints (N x 4 bytes)
- 'aF' : data is a dense array of doubles (N x 8 bytes)
- 'as' : data is a dense array of pointers to string (N x P bytes)
- 'a_' : data is a dense array of pointers to variant (N x P bytes)

Access performance:
because we use fixed size items, random accessed in O(1).


## Dictionary

The data at the pointer address is the dict size, then the keys using fixed size items, then the values using fixed size items.

Access performance:
because we use fixed size items, random accessed
- O(N)     for unsorted dictionary
- O(log(N) for sorted dictionary
- O(1)     for hash sorted dictionary. (TBC)


Hash definition :
(TBC)


# Custom structures

## Enums

'e' + integer array (aB, aW, aI) + string array (ac)

The enum will be coded on the same number of bytes as the integer provided.

## Tuple

't' + [ '@' + name ] + nbFields + type1 + type2 + ...

## Struct / fixed sized record

'r' + [ '@' + name ]
+ name1 + '=' + type1
+ name2 + '=' + type2 + ...
Struct
## Object / variable sized record with optional fields

'R' + [ '@' + name ]
+ name1 + ('='|'?') + type1
+ name2 + ('='|'?') + type2 + ...

Desirable features :
- tag for the type / kind name (to use with factory)
- a struct can inherit through composition (better use pointers to allow base type to grow without breaking derived type layout ?)
- a struct can have optional fields ?  But then it is no longer fixed size
- a struct can accept unknown fields ?
- a struct can specify storage position / order of fields (for compact packing / backward compatibily )


# Variable size types

- string of char / wchar / unicode / utf8 / utf16 / utf32
- variant
- ptr/ref
- nullable : achievable with pointer.

- datetime

- array/list
- dict

- struct
- enum : a kind of struct (uses type metadata)
- tuple : a kind of struct (anonymous fields)

- nullable
- optional


