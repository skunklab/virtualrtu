// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Not exported from index
/** @private */
export class TextMessageFormat {
    static RecordSeparatorCode = 0x1e;
    static RecordSeparator = String.fromCharCode(TextMessageFormat.RecordSeparatorCode);

    static write(output: string): string {
        return `${output}${TextMessageFormat.RecordSeparator}`;
    }

    static parse(input: string): string[] {
        if (input[input.length - 1] !== TextMessageFormat.RecordSeparator) {
            throw new Error("Message is incomplete.");
        }

        const messages = input.split(TextMessageFormat.RecordSeparator);
        messages.pop();
        return messages;
    }
}