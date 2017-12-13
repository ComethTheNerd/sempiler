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

declare class UUID
{
    public constructor();
    //public constructor(uuid : label<'uuid'> | uuid_t)
    public constructor(uuidString : label<'uuidString'> | String)

    //public uuid : uuid_t;
    public uuidString : String;

    public description : String;
    public debugDescription : String;
    //public customMirror : Mirror;
    public hashValue : Int;
}

declare type TimeInterval = Double;

declare class Date
{
    public constructor();
    public constructor(timeIntervalSinceNow : label<'timeIntervalSinceNow'> | TimeInterval);
    public constructor(timeInterval : label<'timeInterval'> | TimeInterval, since : label<'since'> | Date)
    public constructor(timeIntervalSinceReferenceDate : label<'timeIntervalSinceReferenceDate'> | TimeInterval)
    public constructor(timeIntervalSince1970 : label<'timeIntervalSince1970'> | TimeInterval)

    public distantFuture : Date;
    public distantPast : Date;

    public timeIntervalSince(_ : Date);
    public timeIntervalSinceNow : TimeInterval;
    public timeIntervalSinceReferenceDate : TimeInterval;
    public timeIntervalSince1970 : TimeInterval;

    public static timeIntervalSinceReferenceDate : TiemInterval;
    public static timeIntervalbetween1970AndReferenceDate : TimeInterval;

    public description : String;
    public description(_with : label<'with'> | opt<Locale>) : String;
    public debugDescription : String;
    // public customMirror : Mirror;
    public hashValue : Int;
    // public customPlaygroundQuickLook : PlaygroundQuickLook

    public addTimeInterval(_ : TimeInterval) : void;
    public addingTimeInterval(_ : TimeInterval) : Date;
    // public encode(to : label<'to'> | Encoder)
}

declare class NSData extends Array<UInt8>
{
  public constructor(data : label<'data'> | Data);
  public constructor(bytes : label<'bytes'> | Int8[], length : label<'length'> | Int)
  public base64EncodedString(options : label<'options'> | NSData.Base64EncodingOptions = []) : String
}

declare module NSData
{
  class Base64EncodingOptions implements OptionSet
  {
    public static lineLength64Characters : NSData.Base64EncodingOptions;
    public static lineLength76Characters : NSData.Base64EncodingOptions;
    public static endLineWithCarriageReturn : NSData.Base64EncodingOptions;
    public static endLineWithLineFeed : NSData.Base64EncodingOptions;
  }
}

declare function strlen(str : CChar[]) : Int;

@string declare class NSString
{
  public data(using : label<'using'> | UInt) : NSData;
}

declare class URLSessionConfiguration
{
  public static default : URLSessionConfiguration;
  public static ephemeral : URLSessionConfiguration;
  public static background(withIdentifier : label<'withIdentifier'> | String) : URLSessionConfiguration;
}

declare class URLRequest 
{

}

declare class Progress 
{
  
}

declare class JSONSerialization extends NSObject
{

}

declare module JSONSerialization 
{
  class ReadingOptions implements OptionSet
  {

  }
}

declare class URLResponse extends NSObject
{
  public constructor(url : label<'url'> | URL, mimeType : label<'mimeType'> | opt<String>, expectedContentLength : label<'expectedContentLength'> | Int, textEncodingName : label<'textEncodingName'> | opt<String>)

  public expectedContentLength : Int64;
  public suggestedFilename : opt<String>;
  public mimeType : opt<String>;
  public textEncodingname : opt<String>;
  public url : opt<URL>;
}

declare class HTTPURLResponse extends URLResponse
{
  public constructor(url : label<'url'> | URL, statusCode : Int, httpVersion : opt<String>, headerFields : opt<{ [_ : string] : String }>) : terminator

  //public allHeaderFields : dict<AnyHashable, Any>

  public static localizedString(_ : label<'forStatusCode'> | Int) : String

  public statusCode : Int;
}

declare class URLSessionTask extends NSObject
{
  public cancel();
  public resume();
  public suspend();
  public state : URLSessionTask.State;
  public priority : Float;

  public progress : Progress;
  public countOfBytesExpectedToReceive : Int64;
  public countOfBytesReceived : Int64;
  public countOfBytesExpectedToSend : Int64;

  public currentRequest : opt<URLRequest>;
  public originalRequest : opt<URLRequest>;
  public response : opt<URLResponse>;
  public taskDescription : opt<String>;
  public taskIdentifier : Int;
  public error : opt<Error>;

  public earliestBeginDate : opt<Date>;

}

declare module URLSessionTask
{
  enum State
  {
    running,
    suspended,
    canceling,
    completed
  }
}