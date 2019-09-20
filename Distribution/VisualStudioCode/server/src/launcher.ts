
import { spawn, ChildProcess } from 'child_process';
import * as os from 'os';

export interface LaunchResult {
    process: ChildProcess;
    command: string;
}

const launcher = os.platform() === 'win32' ? launchWindows : launchNix;

export async function launch(launchPath : string, cwd: string, args: string[]): Promise<LaunchResult> 
{
    return new Promise<LaunchResult>((resolve, reject) => {
        try
        {
            var result = launcher(launchPath, cwd, args);

            // async error - when target not not ENEOT
            result.process.on('error', err => {
                reject(err);
            });
    
            // success after a short freeing event loop
            setTimeout(function () {
                resolve(result);
            }, 0);
        }
        catch(err)
        {
            reject(err);
        }
    });
}


function launchWindows(launchPath: string, cwd: string, args: string[]): LaunchResult 
{
    function escapeIfNeeded(arg: string) {
        const hasSpaceWithoutQuotes = /^[^"].* .*[^"]/;
        return hasSpaceWithoutQuotes.test(arg)
            ? `"${arg}"`
            : arg.replace("&", "^&");
    }

    let argsCopy = args.slice(0); // create copy of args
    argsCopy.unshift(launchPath);
    argsCopy = [[
        '/s',
        '/c',
        '"' + argsCopy.map(escapeIfNeeded).join(' ') + '"'
    ].join(' ')];

    const process = spawn('cmd', argsCopy, {
        windowsVerbatimArguments: true,
        detached: false,
        cwd,
        windowsHide : true
    });

    return {
        process,
        command: launchPath,
    };
}

function launchNix(launchPath: string, cwd: string, args: string[]): LaunchResult {

    const process = spawn(launchPath, args, {
        detached: false,
        cwd,
        windowsHide : true
    });

    return {
        process,
        command: launchPath
    };
}