import { Range, RangeHelpers } from './range';

export interface Result<T>
{
    value? : T;
    messages? : MessageCollection;
}

export interface MessageCollection
{
    infos? : Message[];
    warnings? : Message[];
    errors? : Message[];
}

export interface Message
{
    kind: MessageKind;
    description : string;
    lineNumber : Range;
    columnIndex : Range;
    pos : Range;
    path? : string;
    tags? : string[];
}

export interface ExceptionMessage extends Message
{
    kind : MessageKind.Error;
    exception : Error;
}

export enum MessageKind
{
    Info = "info",
    Warning = "warning",
    Error = "error"
}

export function isTerminal(result : Result<any>)
{
    return result.messages && result.messages.errors && result.messages.errors.length > 0;
}

export function addMessages(result : Result<any>, messages : MessageCollection)
{
    if(messages)
    {
        var target = result.messages = result.messages || {};
    
        messages.infos && (target.infos = target.infos || []).push.apply(target.infos, messages.infos);
    
        messages.warnings && (target.warnings = target.warnings || []).push.apply(target.warnings, messages.warnings);
        
        messages.errors && (target.errors = target.errors || []).push.apply(target.errors, messages.errors);
    }
}

export function* iterateMessages(messages : MessageCollection) : IterableIterator<Message>
{
    if(messages)
    {
        if(messages.infos)
		{
			for(const info of messages.infos)
			{
				yield info;
			}
		}

		if(messages.warnings)
		{
			for(const warning of messages.warnings)
			{
				yield warning;
			}
		}

		if(messages.errors)
		{
			for(const error of messages.errors)
			{
				yield error;
			}
		}
    }
}

export function addError(result : Result<any>, error : Message)
{
    var messages = result.messages = result.messages || {};

    (messages.errors = messages.errors || []).push(error);
}

export function createErrorFromException(error : Error) : ExceptionMessage
{
    return {
        kind : MessageKind.Error,
        description : error.message,
        exception : error,
        lineNumber : RangeHelpers.Default(),
        columnIndex : RangeHelpers.Default(),
        pos : RangeHelpers.Default()
    }
}

export function print(message : Message) : string
{
    return `${message.kind} : ${message.description}`;     
}

