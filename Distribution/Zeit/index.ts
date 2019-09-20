const { withUiHook, htm } = require('@zeit/integration-utils');
import * as Launcher from './sempiler/launcher';

interface UIHookRequest {
	payload: Payload;
}

type Action = string;

interface Payload {
	action: Action;
	clientState: ClientState;
}

interface ClientState {
	path: string;
}

const compileAction = "compile";
const deployActionPrefix = "deploy-";
const openFileActionPrefix = "open-file-";


let latestSeedPath: string = null;
let latestResult: Diagnostics.Result<Session>;

let deployPort = 5010;



module.exports = withUiHook(async ({ payload }: UIHookRequest) => {
	return htm`
		<Page>
			${await content(payload)}
		</Page>
	`
});


async function content({ action, clientState }: Payload) {
	// payload.clientState": { "dbName": "path/to/sem.ts" }



	if (action.startsWith(openFileActionPrefix)) {
		const filePath = Buffer.from(action.substring(openFileActionPrefix.length), 'base64').toString('ascii');

		try {
			await Launcher.launch("code", __dirname, ["--goto", filePath]);
		}
		catch (err) {
			latestResult = {
				messages: {
					errors: [
						Diagnostics.createErrorFromException(err)]
				}
			};
		}
	}

	if (action.startsWith(deployActionPrefix)) {
		if (latestResult) {
			const artifactName = action.substring(deployActionPrefix.length);

			const session = latestResult.value;

			if (session) {
				if (artifactName in session.artifacts) {
					var artifact = session.artifacts[artifactName];

					if (artifact.targetPlatform === "zeit/now") {
						try {
							const nowDir = session.baseDirectory + "/out/" + artifact.name;

							await Launcher.launch("sh", __dirname, ["./deploy-zeit-now.sh", nowDir, ++deployPort + ""]);
						}
						catch (err) {
							latestResult = {
								messages: {
									errors: [
										Diagnostics.createErrorFromException(err)]
								}
							};
						}
					}
					else if (artifact.targetPlatform === "android") {
						try {
							const androidDir = session.baseDirectory + "/out/" + artifact.name;

							await Launcher.launch("sh", __dirname, ["./deploy-android.sh", androidDir]);
						}
						catch (err) {
							latestResult = {
								messages: {
									errors: [
										Diagnostics.createErrorFromException(err)]
								}
							};
						}
					}
					else {
						latestResult = {
							messages: {
								errors: [
									Diagnostics.createErrorFromException(new Error(`No deployment configuration exists for '${artifact.targetPlatform}'`))]
							}
						};
					}

				}
				else {
					latestResult = {
						messages: {
							errors: [
								Diagnostics.createErrorFromException(new Error(`Artifact '${artifactName}' does not exist in latest compilation session`))]
						}
					};
				}
			}
			else {
				latestResult = {
					messages: {
						errors: [
							Diagnostics.createErrorFromException(new Error("Cannot deploy without successfully completing compilation first"))]
					}
				};
			}
		}
		else {
			latestResult = {
				messages: {
					errors: [
						Diagnostics.createErrorFromException(new Error("Cannot deploy without successfully completing compilation first"))]
				}
			};
		}
	}

	if (action === compileAction) {
		const { path: seedPath } = clientState;

		latestSeedPath = seedPath;

		if (seedPath) {
			const result = latestResult = await compile(seedPath);
		}
		else {
			latestResult = {
				messages: {
					errors: [
						Diagnostics.createErrorFromException(new Error("You must enter a file path"))]
				}
			};
		}
	}


	return htm([renderDefaultView()])
}



function renderSeedPathInput() {
	return `<Input name="path" label="Root project file" value="${latestSeedPath || ""}"/>`
}
function renderCompileButton() {
	return `<Button action="${compileAction}">Compile</Button>`;
}

function renderCompilerControls() {
	return `
	<Box flexDirection="row" display="flex" justifyContent="space-between" alignItems="flex-end">
		${renderSeedPathInput()}
		${renderCompileButton()}
	</Box>`;
}


function renderDefaultView() {
	let content = renderCompilerControls();

	if (latestResult) {
		content += renderCompilationResult(latestResult);
	}
	else {
		content += renderLanding();
	}

	return content;
}

function renderLanding() {
	return `
		<Container>
			<Fieldset>
				<FsContent>
					<H1>Hello</H1>
					<P>Enter the path to your <B>root project file</B> and hit <B>compile</B></P>
				</FsContent>
				${renderFooter()}
			</Fieldset>
		</Container>`
		;
}

function renderFooter()
{
	return `<FsFooter>
		<P>Made for <Link href="https://sempiler.com" target="blank">Sempiler</Link> by <Link href="https://twitter.com/ComethTheNerd" target="blank">Darius</Link></P>
	</FsFooter>`;
}

function renderCompilationResult(result: Diagnostics.Result<Session>) {
	const { messages, value: session } = result;

	let titleSection = `<FsContent>`


	if (Diagnostics.isTerminal(result)) {
		titleSection += `
			<H1>Failed!</H1>
			<P>Issues were detected during compilation</P>`;
	}
	else {
		titleSection += `
			<H1>Success!</H1>
			<P>Compilation completed without errors</P>
			`;
	}


	titleSection += `${renderFileLink(latestSeedPath)}</FsContent>`

	let artifactsSection: string = "";

	if (session) {
		const { artifacts, filesWritten, start, end } = session;

		const artifactCount = Object.keys(artifacts).length;

		const elapsed = (new Date(end).getTime() - new Date(start).getTime()) / 100;

		artifactsSection = `<FsContent>
			<P>
				It took <B>${elapsed} seconds</B> and resulted in <B>${artifactCount === 1 ? "1 artifact" : artifactCount + " artifacts"}</B>
			</P>

			<UL>${Object.keys(artifacts).sort().map(a => renderArtifact(artifacts[a], filesWritten[a] || [])).join('')}</UL>
		</FsContent>`;
	}

	let diagnosticsSection: string = "";

	const hasDiagnostics = messages && (messages.infos && messages.infos.length) || (messages.warnings && messages.warnings.length) || (messages.errors && messages.errors.length);

	if (hasDiagnostics) {
		diagnosticsSection += `<FsContent>${renderDiagnosticMessageCollection(messages)}</FsContent>`;
	}

	return `
		<Container>
			<Fieldset>
				${titleSection}
				${artifactsSection}
				${diagnosticsSection}
				${renderFooter()}
			</Fieldset>
		</Container>`
		;
}

function renderArtifact(artifact: Artifact, filesWritten: string[]) {
	let imgSrc: string = "";

	if (artifact.targetPlatform === "android") {
		imgSrc = "https://pbs.twimg.com/profile_images/875443327835025408/ZvmtaSXW_400x400.jpg";
	}
	else if (artifact.targetPlatform === "zeit/now") {
		imgSrc = "https://pbs.twimg.com/profile_images/1095887975781670912/bHjpwZem_400x400.png"
	}

	// /${artifact.targetPlatform.toLowerCase()}

	return `
	<Container>
		<Fieldset>
			<FsContent>
				<Box display="flex" flexDirection="column">
					<Box display="flex" flexDirection="row" justifyContent="space-between">

					
						<Box>
							<Img src="${imgSrc}" width="30" height="30"/>
							<H2>${capitalize(artifact.name)}</H2>
						</Box>

					
						<Button action="deploy-${artifact.name}">Deploy</Button>
					</Box>

					<P>
						Sempiler emitted <B>${capitalize(artifact.targetLang)}</B> for <B>${capitalize(artifact.targetPlatform)}</B>
					</P>
				</Box>
			</FsContent>
			<FsFooter>
				<Box display="flex" flexDirection="column">
					<B>Files</B>
					<UL>${filesWritten.sort().map(f => renderFileLink(f)).join('<BR/>')}</UL>
				</Box>

			</FsFooter>
		</Fieldset>
	</Container>`;

	//<Link action="doSomething">Do Something</Link>
}


function renderDiagnosticMessageCollection(messageCollection: Diagnostics.MessageCollection) {
	let content = "<UL>";

	if (messageCollection) {
		if (messageCollection.infos) {
			content += messageCollection.infos.map(renderDiagnosticMessage).join('');
		}

		if (messageCollection.warnings) {
			content += messageCollection.warnings.map(renderDiagnosticMessage).join('');
		}

		if (messageCollection.errors) {
			content += messageCollection.errors.map(renderDiagnosticMessage).join('');
		}
	}

	content += "</UL>";

	return content;
}

function renderDiagnosticMessage(message: Diagnostics.Message) {
	let footer: string = "";

	if (message.path) {
		const line = message.lineNumber ? message.lineNumber.start + 1 : null;
		const col = message.columnIndex ? message.columnIndex.start : null;

		footer = `<FsFooter>
			<Box display="flex" flexDirection="column">
				<B>Files</B>
				${renderFileLink(message.path, line, col)} 
			</Box>
		</FsFooter>`
	}

	return `<Fieldset>
		<FsContent>
			<Notice type="${message.kind}">${message.description}</Notice>
		</FsContent>
		${footer}
	</Fieldset>`;
}

function renderFileLink(filePath: string, line?: number, col?: number) {
	const input = `${filePath}:${line > 0 ? line : 0}:${col > 0 ? col : 0}`;

	let label = filePath;

	if (line > 0) {
		label += ` (${line}`;

		if (col > 0) {
			label += `,${col}`;
		}

		label += `)`;
	}

	return `<Link action="${openFileActionPrefix}${Buffer.from(input).toString('base64')}">${label}</Link>`
}

function capitalize(input: string) {
	return input.charAt(0).toUpperCase() + input.substring(1).toLowerCase()
}

import { createSempiler, Sempiler, Session, Artifact, SourceConfig, Diagnostics } from './sempiler';





async function compile(seedPath: string): Promise<Diagnostics.Result<Session>> {
	const result: Diagnostics.Result<Session> = {};

	let sempiler: Sempiler;

	try {
		console.log("Attempting to create Sempiler instance");
		const { messages: m, value } = await createSempiler("", seedPath);
		console.log("Finished creation");
		Diagnostics.addMessages(result, m);

		sempiler = value;


		if (Diagnostics.isTerminal(result)) {
			sempiler && sempiler.dispose();
			console.log("TERMINAL RESULT");
			return result;
		}

		console.log("Attempting compilation");

		const { messages, value: session } = await sempiler.compile();

		console.log("Finished compilation");

		Diagnostics.addMessages(result, messages);

		result.value = session;
	}
	catch (err) {
		Diagnostics.addError(result, Diagnostics.createErrorFromException(err));
	}


	sempiler && sempiler.dispose();

	console.log("DISPOSED");

	return result;
}
