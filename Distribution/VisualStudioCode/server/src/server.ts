'use strict';

import {
	CompletionItem,
	CompletionItemKind,
	createConnection, 
	Connection,
	Diagnostic as VSDiagnostic,
	DiagnosticSeverity as VSDiagnosticSeverity,
	Disposable,
	InitializeResult,
	InitializeError,
	ResponseError,
	TextDocuments, 
	TextDocumentPositionParams,
	ProposedFeatures, 
	TextDocument,
	TextDocumentSyncKind
} from 'vscode-languageserver';

import * as Path from 'path';
import * as Diagnostics from './diagnostics';
import * as FS from 'fs';
import { RangeHelpers } from './range';

import { Sempiler, SourceConfig, SourceLiteral, createSempiler } from './Sempiler';

// const sempiler = new Promise<Diagnostics.Result<Sempiler>>((resolve, reject) => {
	
	// 	createSempiler("","").then(r => {
		
		// 		if(!Diagnostics.isTerminal(r))
		// 		{
			// 			disposables.push(r.value);
			// 		}
			
			// 		resolve(r);
			
			// 	}, reject);
			// });
			// Creates the LSP connection
let connection = createConnection(ProposedFeatures.all);

let disposables : Disposable[] = [];
let sempiler : Sempiler;

// let didSetConfig : boolean = false;

// let hasConfigurationCapability: boolean = false;
// let hasWorkspaceFolderCapability: boolean = false;
// let hasDiagnosticRelatedInformationCapability: boolean = false;


// The workspace folder this server is operating on
let workspacePath: string;
let absMainPath: string;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// [dho] TODO make this configurable!! - 01/05/19
const LanguageID = "typescript";

// Create a manager for open text documents
const documents = new TextDocuments();

// [dho] DISABLING - seems we can `onDidChangeContent` when doc is open so we can just use that - 06/09/18
// live validation
// documents.onDidOpen((event) => {
// 	connection.console.log(`[Server(${process.pid}) ${workspacePath}] Document opened: ${event.document.uri}`);

// 	// [dho] results will be sent back asynchronously - 06/09/18
// 	validateTextDocument(event.document);
// });

const sources : SourceConfig = { 
	path : "", // set in onInitialize
	files : [],
	dirs : [],
	literals : [] 
};

function addLiteralSourceOverride(document : TextDocument)
{	
	if(document.languageId == LanguageID && document.uri)
	{
		const relativePath = Path.relative(workspacePath, fileURIToPath(document.uri));

		for(var i = 0; i < sources.literals.length; ++i)
		{
			if(sources.literals[i].path == relativePath)
			{
				sources.literals[i].text = document.getText();
				return; 
			}
		}

		const literal : SourceLiteral = { path : relativePath, text : document.getText() };

		sources.literals.push(literal);
	}
}

function removeLiteralSourceOverride(document : TextDocument)
{
	if(document.languageId == LanguageID && document.uri)
	{
		const relativePath = Path.relative(workspacePath, fileURIToPath(document.uri));

		for(var i = 0; i < sources.literals.length; ++i)
		{
			if(sources.literals[i].path == relativePath)
			{
				sources.literals.splice(i, 1);
				break; 
			}
		}
	}
}

async function compile() : Promise<Diagnostics.Result<void>>
{
	const result : Diagnostics.Result<void> = {};

	try
	{
		const { messages } = await sempiler.setOverrideSources(sources);
		
		Diagnostics.addMessages(result, messages);
		
		if(!Diagnostics.isTerminal(result))
		{
			const { messages } = await sempiler.compile();
		
			Diagnostics.addMessages(result, messages);
		}
	}
	catch(err)
	{
		Diagnostics.addError(result, Diagnostics.createErrorFromException(err));
	}

	return result;
}

// live validation
documents.onDidChangeContent(change => {
	
	connection.console.log(`[Server(${process.pid}) ${workspacePath}] Document changed: ${change.document.uri}`);
	
	addLiteralSourceOverride(change.document);
	
	// [dho] results will be sent back asynchronously - 01/09/18
	validateTextDocument(change.document);
});

// compilation
documents.onDidSave(async saved => {
	
	connection.console.log(`[Server(${process.pid}) ${workspacePath}] Document saved: ${saved.document.uri}`);

	// [dho] if this file is referenced in the source then its content will be 
	// read from the disk. NOTE reading from disk is less efficient potentially,
	// but we do not want to pass files that may never get referenced? - 01/05/19
	removeLiteralSourceOverride(saved.document);

	// if(fileURIToPath(saved.document.uri) === semConfigPath)
	// {
	// 	const result : Diagnostics.Result<void> = {};

	// 	try
	// 	{
	// 		const config : Config = JSON.parse(saved.document.getText());

	// 		const { messages } = await sempiler.setConfig(config);
			
	// 		Diagnostics.addMessages(result, messages);
	// 	}
	// 	catch(err)
	// 	{
	// 		Diagnostics.addError(result, Diagnostics.createErrorFromException(err));
	// 	}
		
	// 	connection.console.log("Updating Sempiler config...");

	// 	logMessages(connection, result.messages);

	// 	if(Diagnostics.isTerminal(result))
	// 	{
	// 		connection.window.showErrorMessage("Failed to update sempiler config. Please check the log for details.");
	// 	}
	// 	else
	// 	{
	// 		connection.console.log("Success!");
	// 		didSetConfig = true;
	// 	}
	// }
	// else if(didSetConfig)
	// {
		const result : Diagnostics.Result<void> = {};

		const filePath =  Path.relative(workspacePath, fileURIToPath(saved.document.uri));

		const { messages } = await compile();
		
		Diagnostics.addMessages(result, messages);

		connection.console.log(`Compiling '${filePath}'...`);

		logMessages(connection, result.messages);

		sendVSDiagnostics(result.messages);

		if(!Diagnostics.isTerminal(result))
		{
			connection.console.log("Success!");
		}
	// }
});

documents.listen(connection);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

function fileURIToPath(uri : string)
{
	return uri && uri.startsWith('file://') ? uri.substring('file://'.length) : uri;
}

function logMessages(connection : Connection, messages : Diagnostics.MessageCollection)
{
	for(const message of Diagnostics.iterateMessages(messages))
	{
		const description = '[' + ((message.tags && message.tags.join(" ")) || 'sempiler') + '] ' + message.description;
		
		switch(message.kind)
		{
			case Diagnostics.MessageKind.Info:
				connection.console.info(description);
			break;

			case Diagnostics.MessageKind.Warning:
				connection.console.warn(description);
			break;

			case Diagnostics.MessageKind.Error:
				connection.console.error(description);
			break;
		}
	}
}


let prevDiagnostics : { [uri : string] : VSDiagnostic[] } = {};

function sendVSDiagnostics(messages : Diagnostics.MessageCollection)
{
	const diagnostics = createVSDiagnostics(messages);

	for(const path in diagnostics)
	{
		connection.sendDiagnostics({
			uri :`file://${path}`, 
			diagnostics : diagnostics[path]
		});
		
		delete prevDiagnostics[path];
	}
	
	// [dho] clear out any stale diagnostics - 02/05/19
	for(const path in prevDiagnostics)
	{
		connection.sendDiagnostics({
			uri :`file://${path}`, 
			diagnostics : []
		});
	}

	prevDiagnostics = diagnostics;
}

function createVSDiagnostics(messages : Diagnostics.MessageCollection) : { [uri : string] : VSDiagnostic[] }
{	
	var result : { [uri : string] : VSDiagnostic[] } = {};

	for(var message of Diagnostics.iterateMessages(messages))
	{
		if(message.path)
		{
			const { lineNumber, columnIndex } = message;

			if(RangeHelpers.IsValid(lineNumber) && RangeHelpers.IsValid(columnIndex))
			{
				let severity : VSDiagnosticSeverity;

				switch(message.kind)
				{
					case Diagnostics.MessageKind.Info:
						severity = VSDiagnosticSeverity.Information;
					break;

					case Diagnostics.MessageKind.Warning:
						severity = VSDiagnosticSeverity.Warning;
					break;
					
					case Diagnostics.MessageKind.Error:
						severity = VSDiagnosticSeverity.Error;
					break;

					default:
						severity = VSDiagnosticSeverity.Hint;
					break;
				}

				(result[message.path] = result[message.path] || []).push({
					severity,
					range: {
						start: { line : lineNumber.start, character : columnIndex.start },
						end: { line : lineNumber.end, character : columnIndex.end }
					},
					message: message.description,
					source: (message.tags && message.tags.join(" ")) || 'sempiler'
				});
			}
		}
	}

	return result;
}

// function isConfig(textDocument: TextDocument) : boolean
// {
// 	if(textDocument.uri && textDocument.uri.startsWith(workspacePath))
// 	{
// 		const p = textDocument.uri.split(Path.sep);

// 		return p[p.length - 1] === 'semconfig.json';
// 	}

// 	return false;
// }

async function validateTextDocument(_: TextDocument): Promise<void> 
{	
	/*if(isConfig(textDocument))
	{
		// [dho] don't bother validating? when it's saved it will get sent to sempiler
		// and if it fails we will deliver the diagnostics then
	}
	else *///if(didSetConfig)
	{
		const result : Diagnostics.Result<void> = {};
		try
		{
			const { messages } = await compile();
		
			Diagnostics.addMessages(result, messages);
		}
		catch(err)
		{
			Diagnostics.addError(result, Diagnostics.createErrorFromException(err));
		}

		
		sendVSDiagnostics(result.messages);
	}
	
	// In this simple example we get the settings for every validate run.
	// let settings = await getDocumentSettings(textDocument.uri);


}


connection.onInitialize(async (params/*, token*/) => {

	var result : Diagnostics.Result<void> = {};

	if(disposables)
	{
		disposables.forEach(d => d.dispose());
	}

	disposables = [];

	// [dho] NOTE `params.rootUri` will have `file://` prefix and will cause issues like `spawn ENOENT`
	// if used as the cwd. `params.rootPath` will be a path without that `file://` prefix and is more appropriate
	// for our use case generally - 05/09/18 
	sources.path = workspacePath = params.rootPath;
// (global as any).logXXXX = (m : string) => connection.console.log(m);
	connection.console.log(`[Server(${process.pid}) ${workspacePath}] Started and initialize received`);
	
	absMainPath = `${workspacePath}${Path.sep}sem.ts`;

	connection.console.log(`Sempiler main file path : '${absMainPath}'`);

	try
	{
		// [dho] here we want to disable the automatic validation that VSCode enables by
		// default, because the validation on the source files will be done by Sempiler - 01/05/19
		{
			// [dho] TODO support other languages - 01/05/19
			const reqSempilerVSCodeSettings = {
				"javascript.validate.enable": false,
				"typescript.validate.enable": false
			};
	
			const settingsDirPath = `${workspacePath}${Path.sep}.vscode`
			const settingsJSONPath = `${settingsDirPath}${Path.sep}settings.json`;
			
			if(FS.existsSync(settingsJSONPath))
			{
				const settings = JSON.parse(FS.readFileSync(settingsJSONPath, 'utf8'));
			
				FS.writeFileSync(settingsJSONPath, JSON.stringify({
					...settings,
					...reqSempilerVSCodeSettings
				}, null, 4));
			}
			else
			{
				if(!FS.existsSync(settingsDirPath))
				{
					FS.mkdirSync(settingsDirPath);
				}

				FS.writeFileSync(settingsJSONPath, JSON.stringify(reqSempilerVSCodeSettings, null, 4));
			}
		}

		if(!FS.existsSync(absMainPath))
		{
			FS.writeFileSync(absMainPath, `
	function helloWorld() {
		System.out.println("Hello World!");
	}

	#run helloWorld();
			`);
		}

		const { messages, value } = await createSempiler(workspacePath, absMainPath);

		Diagnostics.addMessages(result, messages);
		
		value && disposables.push(sempiler = value);

		sempiler.onError(data => {
			if(data)
			{
				connection.window.showErrorMessage(data);
				
				connection.console.log(`ERROR : '${data}'`);
			}
		})

		// sempiler.onLog(data => {
		// 	if(data)
		// 	{
		// 		connection.console.log(data);
		// 	}
		// })
	}
	catch(err)
	{
		Diagnostics.addError(result, Diagnostics.createErrorFromException(err));
	}

	logMessages(connection, result.messages);

	if(Diagnostics.isTerminal(result))
	{
		return new ResponseError<InitializeError>(1, "Failed to initialize Sempiler. Please check the log for details.", { retry: false })
	}
	else
	{
		connection.console.log("Sempiler ready to go!");


		const initResult : InitializeResult = {
			capabilities: {
				textDocumentSync: {
					openClose: true,
					change: TextDocumentSyncKind.Full,
					save : { includeText : false }
				},
				// Tell the client that the server supports code completion
				completionProvider: {
					resolveProvider: true
				}
			}
		};
	
		return initResult;
	}
});

// This handler provides the initial list of the completion items.
connection.onCompletion(
	async (_textDocumentPosition: TextDocumentPositionParams): Promise<CompletionItem[]> => {
	// The pass parameter contains the position of the text document in
	// which code complete got requested. For the example we ignore this
	// info and always provide the same completion items.
	connection.console.log(`[Server(${process.pid}) ${workspacePath}] asked for completions`);

	// [dho] This only works if I start typing something and it can autocomplete, eg. 'T' it will give me 'TypeScript', and 
	// likewise if I type 'J' it gives 'JavaScript'

	// [dho] test shows it supports async completions! - 31/08/18
	await new Promise(r => setTimeout(r, 300));

	return [
		// [dho] lets return empty array here if that disables intellisense for the source language? - 31/08/18
		{
			label: 'TypeScript',
			kind: CompletionItemKind.Text,
			data: 1
		},
		{
			label: 'JavaScript',
			kind: CompletionItemKind.Text,
			data: 2
		}
		];
	}
);


// This handler resolves additional information for the item selected in
// the completion list.
connection.onCompletionResolve(
	(item: CompletionItem): CompletionItem => {
		if (item.data === 1) {
			item.detail = 'TypeScript details';
			item.documentation = 'TypeScript documentation';
		} else if (item.data === 2) {
			item.detail = 'JavaScript details';
			item.documentation = 'JavaScript documentation';
		}
		return item;
	}
);

connection.onShutdown(() => disposables.forEach(d => d.dispose()));

connection.listen();

