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

declare type ptr<T> = number & { pointee : T }

declare function addressof<T>(t : T) : ptr<T>;

declare function auto<T>(t: T): T;

declare function heap<T>(t : T) : ptr<T>; // const foo = heap(new Foo())
/// <reference no-default-lib="true"/>

declare type preproc = undefined;
/// <reference no-default-lib="true"/>

declare type auto = any;
declare type char = number;
declare type double = number;
declare type float = number;
declare type int = number;
declare type long = number;
declare type uint = number;
declare type size_t = uint;

//declare function asm(literal : string) : void;

/**
 * USAGE:
 * var j = extern<{ foo : () => void }>("C");
 * ...
 * j.foo();
 */
//declare function extern<T>(literal? : string) : T;

/**
 * "foo"; // label
 * ...
 * goto("foo"); 
 */
//declare function goto(label : string) : void;

//https://docs.microsoft.com/en-us/cpp/cpp/lvalues-and-rvalues-visual-cpp
declare type lval<T> = T
declare type rval<T> = T

declare function calloc(num : size_t, size : size_t) : ptr<void>;

declare function free(p : ptr<void>) : void;

declare function malloc(size : size_t) : ptr<void>;

declare function realloc(p : ptr<void>, size : size_t) : ptr<void>;

declare function sizeof(t : any) : size_t;

// declare function inline(target: any, propertyKey: string, descriptor: any/*PropertyDescriptor*/);

/*type*/interface Array<T>/* = ptr<T> &*/ {
    /** Iterator */
    [Symbol.iterator](): IterableIterator<T>;
    [n: number]: T;
}

// http://www.cplusplus.com/reference/string/string/
interface String
{
    c_str() : char[];
    copy(s : char[], len : size_t, pos : size_t = 0) : size_t;
    data() : char[];
    // [dho] @TODO - 19/09/17
    //get_allocator() : allocator_type //allocator<char> 
}
/// <reference no-default-lib="true"/>

//ISO C99 and ISO C++14 standards
// https://gcc.gnu.org/onlinedocs/cpp/Standard-Predefined-Macros.html#Standard-Predefined-Macros
declare const __ASSEMBLER__ : int/* | undefined*/;
declare const __cplusplus : int/* | undefined*/;
declare const __DATE__ : string;
declare const __FILE__ : string;
declare const __LINE__ : int;
declare const __OBJC__ : int/* | undefined*/;
declare const __STDC__ : int/* | undefined*/;
declare const __STDC_HOSTED__ : int;
declare const __STDC_VERSION__ : long;
declare const __STDCPP_THREADS__ : int;
declare const __TIME__ : string;

// Microsoft specific
// https://msdn.microsoft.com/en-us/library/b0084kay.aspx
declare const __ATOM__ : int/* | undefined*/;
declare const __AVX__ : int/* | undefined*/;
declare const __AVX2__ : int/* | undefined*/;
declare const _CHAR_UNSIGNED : int/* | undefined*/;
declare const __CLR_VER : int;
declare const _CONTROL_FLOW_GUARD : int/* | undefined*/;
declare const __COUNTER__ : int;
declare const __cplusplus_cli : int/* | undefined*/;
declare const __cplusplus_winrt : int/* | undefined*/;
declare const _CPPRTTI : int/* | undefined*/;
declare const _CPPUNWIND : int/* | undefined*/;
declare const _DEBUG : int/* | undefined*/;
declare const _DLL : int/* | undefined*/;
declare const __FUNCDNAME__ : string;
declare const __FUNCSIG__ : string;
declare const __FUNCTION__ : string;
declare const _INTEGRAL_MAX_BITS : int;
declare const __INTELLISENSE__ : int/* | undefined*/;
declare const _ISO_VOLATILE : int/* | undefined*/;
declare const _KERNEL_MODE : int/* | undefined*/;
declare const _M_AMD64 : int/* | undefined*/;
declare const _M_ARM : int/* | undefined*/;
declare const _M_ARM_ARMV7VE : int/* | undefined*/;
declare const _M_ARM_FP : int/* | undefined*/;
declare const _M_ARM64 : int/* | undefined*/;
declare const _M_CEE : int/* | undefined*/;
declare const _M_CEE_PURE : int/* | undefined*/;
declare const _M_CEE_SAFE : int/* | undefined*/;
declare const _M_FP_EXCEPT : int/* | undefined*/;
declare const _M_FP_FAST : int/* | undefined*/;
declare const _M_FP_PRECISE : int/* | undefined*/;
declare const _M_FP_STRICT : int/* | undefined*/;
declare const _M_IX86 : int/* | undefined*/;
declare const _M_IX86_FP : int/* | undefined*/;
declare const _M_X64 : int/* | undefined*/;
declare const _MANAGED : int/* | undefined*/;
declare const _MSC_BUILD : int;
declare const _MSC_EXTENSIONS : int/* | undefined*/;
declare const _MSC_FULL_VER : int;
declare const _MSC_VER : int;
declare const _MSVC_LANG : int/* | undefined*/;
declare const __MSVC_RUNTIME_CHECKS : int/* | undefined*/;
declare const _MT : int/* | undefined*/;
declare const _NATIVE_WCHAR_T_DEFINED : int/* | undefined*/;
declare const _OPENMP : int/* | undefined*/;
declare const _PREFAST_ : int/* | undefined*/;
declare const __TIMESTAMP__ : string;
declare const _VC_NODEFAULTLIB : int/* | undefined*/;
declare const _WCHAR_T_DEFINED : int/* | undefined*/;
declare const _WIN32 : int/* | undefined*/;
declare const _WIN64 : int/* | undefined*/;
declare const _WINRT_DLL : int/* | undefined*/;

// OSX : https://developer.apple.com/library/content/documentation/Porting/Conceptual/PortingUnix/compiling/compiling.html#//apple_ref/doc/uid/TP40002850-SW13
declare const __APPLE__ : int/* | undefined*/;
declare const __APPLE_CC__ : int/* | undefined*/;
declare const __BIG_ENDIAN__ : int/* | undefined*/;
declare const __LITTLE_ENDIAN__ : int/* | undefined*/;
declare const __MACH__ : int/* | undefined*/;
declare const __NATURAL_ALIGNMENT__ : int/* | undefined*/;

