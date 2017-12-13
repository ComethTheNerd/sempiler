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

declare class SessionManager 
{
    public constructor(
        configuration? : label<'configuration'> | URLSessionConfiguration,
        delegate? : SessionDelegate,
        serverTrustPolicyManager? : opt<ServerTrustPolicyManager>
    )

    public request(
        url : URLConvertible, 
        method? : label<'method'> | HTTPMethod, 
        parameters? : label<'parameters'> | opt<Parameters>, 
        encoding? : label<'encoding'> | ParameterEncoding,
        headers? : label<'headers'> | opt<HTTPHeaders>
    ) : DataRequest
    
    public retrier: opt<RequestRetrier>

}

declare type Parameters = { [_ : string] : Any }

declare interface ParameterEncoding 
{

}

// [dho] using `@string` for now to allow for converting
// from strings to `URLConvertible` implicitly - 09/12/17
@string declare interface URLConvertible 
{

}

declare class ServerTrustPolicyManager
{

}

declare class Request 
{
    public task: opt<URLSessionTask>

}

declare module Request 
{
    type ProgressHandler = (_ : Progress) => Void;

    // [dho] this is actually an enum but because failure
    // comes with an Error parameter and we need a way of modelling that - 08/12/17
    class ValidationResult 
    {
        // public static success : Request.ValidationResult;
        // public static failure(_ : Error) : Request.ValidationResult;
    }
}

declare class SessionDelegate extends NSObject
{

}

declare type HTTPHeaders = { [_ : string] : String }

declare enum HTTPMethod 
{
    options,
    get,
    head,
    post,
    put,
    patch,
    delete,
    trace,
    connect
}

declare class DataRequest extends Request
{
    public request : opt<URLRequest>
    public progress : Progress
    
    public validate(validation : DataRequest.Validation) : DataRequest;
    public validate<S extends Sequence<Int>>(acceptableStatusCodes : label<'statusCode'> | S) : DataRequest
    // [dho] @TODO I'm pretty sure this will be ambiguous and so we need to implement the named arguments - 08/12/17
    //public validate<S extends Sequence<Int>>(acceptableContentTypes : label<'contentType'> | S) : DataRequest
    public validate() : DataRequest

    public responseJSON(
        queue? : label<'queue'> | opt<DispatchQueue>,
        options? : label<'options'> | JSONSerialization.ReadingOptions,
        completionHandler : label<'completionHandler'> | ((dr : DataResponse<Any>) => Void)
    ) : DataRequest

    public responseJSON(
        completionHandler : label<'completionHandler'> | ((dr : DataResponse<Any>) => Void)
    ) : DataRequest
}

declare module DataRequest 
{
    type Validation = (req : opt<URLRequest>, res : opt<HTTPURLResponse>, data : opt<Data>) => ValidationResult
}

declare class DataResponse<T> {

    public constructor(
        request : label<'request'> | opt<URLRequest>,
        response : label<'response'> | opt<HTTPURLResponse>,
        data : label<'data'> | opt<Data>,
        result : Result<T>,
        timeline? : Timeline
    )

    public request : opt<URLRequest>;
    public response : opt<HTTPURLResponse>;
    public data : opt<Data>;
    public result : Result<T>;
    public timeline : Timeline;
    public value : opt<Value>;
    public error : opt<Error>;
}

// [dho] this is actually an enum but because failure
// comes with an Error parameter and we need a way of modelling that - 08/12/17    
declare class Result<T>
{
    // public static success(value : T) : Result<T>
    // public static failure(error : Error) : Result<T>

    public isSuccess : Bool
    public isFailure : Bool

    public value : opt<T>
    public error : opt<Error>
}

declare interface RequestAdapter
{
    adapt(urlRequest : URLRequest) : URLRequest
}

declare interface RequestRetrier
{
    should(
        manager : SessionManager,
        request : label<'retry'> | Request,
        error : label<'with'> | Error,
        completion : label<'completion'> | RequestRetryCompletion
    )
}

declare type RequestRetryCompletion = (shouldRetry : Bool, timeDelay : TimeInterval) => Void