// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

import { ScheduleCoordinatorNotificationType } from "@Obsidian/Enums/Group/scheduleCoordinatorNotificationType";
import { FieldType } from "@Obsidian/SystemGuids/fieldType";
import { TimePickerValue } from "@Obsidian/ViewModels/Controls/timePickerValue";
import { PublicEditableAttributeBag } from "@Obsidian/ViewModels/Utility/publicEditableAttributeBag";

/**
 * Converts a 0-1 decimal multiplier into the 0-100 percent integer the
 * NumberBox edit input expects. Null stays null.
 */
export function percentToInt(value: number | null | undefined): number | null {
    if (value == null) {
        return null;
    }
    return Math.round(value * 100);
}

/**
 * Inverse of percentToInt: converts a 0-100 percent input back into a 0-1
 * decimal for persistence. Null stays null.
 */
export function intToPercent(value: number | null | undefined): number | null {
    if (value == null) {
        return null;
    }
    return value / 100;
}

/**
 * Formats a 0-1 decimal multiplier as a "NN%" placeholder string used on
 * the override inputs to show the inherited group-type value.
 */
export function formatPercent(value: number | undefined | null): string {
    if (value == null) {
        return "";
    }
    return `${Math.round(value * 100)}%`;
}

/**
 * Converts the Group's ScheduleCoordinatorNotificationTypes flag bitmask
 * into a list of stringified flag values for the CheckBoxList.
 */
export function extractFlagValues(flags: number): string[] {
    const result: string[] = [];
    if ((flags & ScheduleCoordinatorNotificationType.Decline) !== 0) {
        result.push(ScheduleCoordinatorNotificationType.Decline.toString());
    }
    if ((flags & ScheduleCoordinatorNotificationType.Accept) !== 0) {
        result.push(ScheduleCoordinatorNotificationType.Accept.toString());
    }
    if ((flags & ScheduleCoordinatorNotificationType.SelfSchedule) !== 0) {
        result.push(ScheduleCoordinatorNotificationType.SelfSchedule.toString());
    }
    return result;
}

/**
 * Inverse of extractFlagValues: combines stringified flag values back into
 * a single bitmask for persistence.
 */
export function combineFlagValues(values: string[]): number {
    let combined = 0;
    for (const v of values) {
        combined |= parseInt(v, 10) || 0;
    }
    return combined;
}

/**
 * Converts a tri-state boolean (true / false / null) into the
 * "true" / "false" / "" string the RadioButtonList expects.
 */
export function triStateToString(value: boolean | null | undefined): string {
    if (value == null) {
        return "";
    }
    return value ? "true" : "false";
}

/**
 * Inverse of triStateToString. Empty string and unknown values map to null.
 */
export function stringToTriState(value: string): boolean | null {
    if (value === "true") {
        return true;
    }
    if (value === "false") {
        return false;
    }
    return null;
}

/**
 * Parses a string into a non-negative integer. Empty, whitespace, and
 * unparseable inputs map to null.
 */
export function nullableInt(value: string): number | null {
    if (!value) {
        return null;
    }
    const n = parseInt(value, 10);
    return Number.isNaN(n) ? null : n;
}

/**
 * Parses an ISO-8601 time-of-day string (e.g., "13:30:00") into the
 * TimePickerValue object the &lt;TimePicker&gt; expects ({ hour, minute }).
 * Returns an empty object when the input is empty.
 */
export function parseTimeString(value: string | null | undefined): TimePickerValue {
    if (!value) {
        return {};
    }
    const parts = value.split(":");
    const hour = parseInt(parts[0] ?? "", 10);
    const minute = parseInt(parts[1] ?? "", 10);
    return {
        hour: Number.isNaN(hour) ? undefined : hour,
        minute: Number.isNaN(minute) ? undefined : minute
    };
}

/**
 * Inverse of parseTimeString: converts a TimePickerValue back into an
 * ISO-8601 time-of-day string ("HH:mm:ss"). Returns null when the input
 * has no hour or minute.
 */
export function formatTimeValue(value: TimePickerValue): string | null {
    if (value.hour == null || value.minute == null) {
        return null;
    }
    const hh = value.hour.toString().padStart(2, "0");
    const mm = value.minute.toString().padStart(2, "0");
    return `${hh}:${mm}:00`;
}

/**
 * Creates a new attribute instance suitable for editing with the AttributeEditor control.
 * Centralized to reduce the risk of fixing attribute-default bugs in one place but not another.
 */
export function createNewAttribute(): PublicEditableAttributeBag {
    return {
        guid: "",
        name: "",
        description: "",
        isActive: true,
        isPublic: false,
        isRequired: false,
        isShowOnBulk: false,
        isShowInGrid: false,
        isAnalytic: false,
        isAnalyticHistory: false,
        isAllowSearch: false,
        isEnableHistory: false,
        isIndexEnabled: false,
        isSystem: false,
        fieldTypeGuid: FieldType.Text,
        configurationValues: {},
        categories: [],
        key: "",
        abbreviatedName: "",
        preHtml: "",
        postHtml: "",
        defaultValue: "",
        isSuppressHistoryLogging: false,
        attributeColor: "",
        iconCssClass: ""
    };
}
