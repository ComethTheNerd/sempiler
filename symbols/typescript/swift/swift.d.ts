/**
BSD License

For Sempiler software

Copyright (c) 2017-present, Quantum Commune Ltd. All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

 * Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

 * Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

 * Neither the name Quantum Commune nor the names of its contributors may be used to
   endorse or promote products derived from this software without specific
   prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

/// <reference no-default-lib="true"/>

// declare const NULL : any;

/**
 * eg. i : volatile | int
 */
//declare type volatile = undefined;

// readonly instead of const because const can't be used in union
declare type readonly = undefined;

// declare type constant = undefined;

//declare type mutable = undefined;

declare type inline = undefined;

declare type override = undefined;


// declare type macro = undefined;

//declare type unsigned = undefined;

//declare type virtual = undefined;

declare function struct(target : any) : any;

declare function string(target : any) : any;
declare function number(target : any) : any;
declare function boolean(target : any) : any;

/////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////
// [dho] need to include the following declarations so people can use for..of on an array, otherwise
// intellisense complains - 18/08/17
//
// [dho] @TODO find a way of not exposing this Symbol var in the ambient scope
interface SymbolConstructor {
    /**
     * A method that returns the default iterator for an object. Called by the semantics of the
     * for-of statement.
     */
    readonly iterator: symbol;
}
declare var Symbol: SymbolConstructor;

interface IteratorResult<T> {
    done: boolean;
    value: T;
}

interface Iterator<T> {
    next(value?: any): IteratorResult<T>;
    return?(value?: any): IteratorResult<T>;
    throw?(e?: any): IteratorResult<T>;
}

interface Iterable<T> {
    [Symbol.iterator](): Iterator<T>;
}

interface IterableIterator<T> extends Iterator<T> {
    [Symbol.iterator](): IterableIterator<T>;
}
/// <reference no-default-lib="true"/>

/** Assign an alternate argument label from the parameter identifier */
declare type label<T extends string> = undefined;
/// <reference no-default-lib="true"/>

declare type ptr<T> = number & { pointee : T }

declare function addressof<T>(t : T) : ptr<T>;

declare function auto<T>(t: T): T;

declare function heap<T>(t : T) : ptr<T>; // const foo = heap(new Foo())
/// <reference no-default-lib="true"/>

/** for declaring built in dictionary constructs that have a
 * key type that is not string
 */
declare type dict<K, V> = { [k:string] : V }
/// <reference no-default-lib="true"/>

declare type opt<T> = T
/// <reference no-default-lib="true"/>

/** This fakeyword is used to signify a function is 'required'
 * in languages like Swift
 */
declare type required = undefined;
/// <reference no-default-lib="true"/>

/** This fakeyword is used to signify a function call terminates
 * program execution unconditionally, eg `die()` in PHP or `fatalError()` in Swift,
 * which is not semantically the same as throwing an exception as that
 * might be caught.
 */
declare type terminator = undefined;
/// <reference no-default-lib="true"/>

declare class timespec
{
    
}

/// <reference no-default-lib="true"/>

declare type ref = undefined;
declare type escaping = undefined;
declare type noescaping = undefined;

declare function UIApplicationMain(target : any) : any;

declare function fatalError(message : string) : terminator | void
declare function print(_ : string, separator? : label<'separator'> | string, terminator? : label<'terminator'> | string)
// declare type inout = undefined;
// declare type weak = undefined;

declare class Array<T>
{
    constructor(sequence : T);
    constructor(element : label<'repeating'> | Any, count : label<'count'> | Int)

    /** Iterator */
    [Symbol.iterator](): IterableIterator<T>;
    [n: number]: T;

    count : Int;
    isEmpty : Bool;
    capacity : Int;

    first : opt<T>
    last : opt<T>

    append(_ : T) : void;
    insert(_ : T, at : label<'at'>| Int) : void;
    reserveCapacity(minimumCapacity : Int);

    //sort(by areInIncreasingOrder: (Element, Element) throws -> Bool)

    sorted(areInIncreasingOrder : label<'by'> | ((t1 : T, t2 : T) => Bool)) : Array<T>;
}


interface ContiguousArray<T>
{
    constructor();
    constructor(element : label<'repeating'> | Any, count : Int)
    
    /** Iterator */
    [Symbol.iterator](): IterableIterator<T>;
    [n: number]: T;

    count : Int;
    isEmpty : Bool;
    capacity : Int;

    first : opt<T>
    last : opt<T>
}

interface Set<T>
{
    /** Iterator */
    [Symbol.iterator](): IterableIterator<T>;
    count : Int;
    isEmpty : Bool;
    insert(_:T): void;
    remove(_:T): opt<T>;
    removeAll() : void;
    intersection(_:Set<T>) : Set<T>
    subtracting(_:Set<T>) : Set<T>
    symmetricDifference(_:Set<T>) : Set<T>
    union(_:Set<T>) : Set<T>
    sorted() : Set<T>
}

interface OptionSet {

}

declare interface NSObjectProtocol {
    isEqual(_ : opt<Any>) : Bool;
    hash : Int;
    self() : Self;
}

declare type Self = any;

declare class NSObject {
    static initialize() : void;
    static load();
    static superclass();

    constructor();

    copy():NSObject;
    mutableCopy():NSObject;

    isEqual(other : opt<any>)
}

declare type NSMutableDictionary = { [key : string] : Any };

declare type NSNumber = number;

declare class NSCoder {}


declare interface Error {
    localizedDescription : string;
}

declare class NSError implements Error {
    
}

declare type Any = any;

declare type Void = void;

// https://developer.apple.com/documentation/foundation/data
declare class Data extends NSData
{
    public constructor<S>(_ : S);
}

declare class Bundle {}

@boolean declare class Bool 
{

}

@number declare class Int 
{
    public constructor<T>(_ : T)
    public negate();
}

@number declare class Int8 
{
    public magnitude : Int8;
}

declare type CChar = Int8;

@number declare class Int64
{

}

@number declare class Double 
{
    public magnitude() : Double;
}

@number declare class Float 
{

}

@number declare class UInt 
{
    public magnitude() : UInt;
}

@number declare class UInt8
{
    public magnitude : Int8;
}

declare type CUnsignedChar = UInt8;

@number declare class UInt16
{

}

@number declare class UInt64
{

}

declare type uint64_t = number;

@string declare class String 
{
    public constructor()
    public constructor<T>(_ : T)
    public constructor(repeating : label<'repeating'> | String, count : label<'count'> | Int)

    public isEmpty : Bool;
    
    public utf8 : String.UTF8View;
    public utf8CString : ContiguousArray<CChar>
    public lengthOfBytes(using : label<'using'> | String.Encoding) : Int;

    public cString(using : label<'using'> | String.Encoding) : opt<Int8[]>
    public data(using : label<'using'> | String.Encoding) : NSData;
}

declare module String 
{
    class Encoding
    {
        constructor(rawValue : label<'rawValue'> | UInt)

        public rawValue : UInt;

        public static ascii : String.Encoding;
        public static iso2022JP : String.Encoding;
        public static iosLatin1 : String.Encoding;
        public static isoLatin2 : String.Encoding;
        public static japaneseEUC : String.Encoding;
        public static macOSRoman : String.Encoding;
        public static nextstep : String.Encoding;
        public static nonLossyASCII : String.Encoding;
        public static shiftJIS : String.Encoding;
        public static symbol : String.Encoding;
        public static unicode : String.Encoding;
        public static utf16 : String.Encoding;
        public static utf16BigEndian : String.Encoding;
        public static utf16LittleEndian : String.Encoding;
        public static utf32 : String.Encoding;
        public static utf32BigEndian : String.Encoding;
        public static utf32LittleEndian : String.Encoding;
        public static utf8 : String.Encoding;
        public static windowsCP1250 : String.Encoding;
        public static windowsCP1251 : String.Encoding;
        public static windowsCP1252 : String.Encoding;
        public static windowsCP1253 : String.Encoding;
        public static windowsCP1254 : String.Encoding;
    }

    @string class UTF8View 
    {

    }
}

declare class CFDictionary { }

declare class UnsafePointer<Pointee>
{
    public constructor(_ : OpaquePointer);
    public constructor(_ : opt<OpaquePointer>);
    public constructor(_ : opt<UnsafeMutablePointer<Pointee>>)
    public constructor(_ : UnsafeMutablePointer<Pointee>)
    public constructor(_ : UnsafePointer<Pointee>)
    public constructor(_ : opt<UnsafePointer<Pointee>>)
    public constructor(bitPattern : label<'bitPattern'> | UInt);
    public constructor(bitPattern : label<'bitPattern'> | Int);

    // TODO
}

declare type Pointee = any;

declare class OpaquePointer
{
    // TODO
}

declare class UnsafeMutablePointer<T>
{
    // TODO
}

// declare module UnsafePointer
// {
//     type Distance = UnsafePointer;
// }
