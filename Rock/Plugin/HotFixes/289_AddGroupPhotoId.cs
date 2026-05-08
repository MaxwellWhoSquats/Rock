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

namespace Rock.Plugin.HotFixes
{
    /// <summary>
    /// Plug-in migration
    /// </summary>
    /// <seealso cref="Rock.Plugin.Migration" />
    [MigrationNumber( 289, "19.0" )]
    public class AddGroupPhotoId : Migration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            ME_AddGroupPhotoId_Up();
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // Down migrations are not yet supported in plug-in migrations.
        }

        /// <summary>
        /// Backs the GroupDetail Overview hero image. Mirrors
        /// <c>Person.PhotoId</c>; orphan cleanup is handled by the
        /// <c>BinaryFile.IsTemporary</c> toggle. Ships as a plug-in migration
        /// because v19.0 is locked on a separate branch.
        /// </summary>
        private void ME_AddGroupPhotoId_Up()
        {
            Sql( @"
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID( N'[dbo].[Group]' )
        AND [name] = 'PhotoId'
)
BEGIN
    ALTER TABLE [dbo].[Group] ADD [PhotoId] [int] NULL;
END" );

            Sql( @"
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = 'FK_dbo.Group_dbo.BinaryFile_PhotoId'
        AND [parent_object_id] = OBJECT_ID( N'[dbo].[Group]' )
)
BEGIN
    ALTER TABLE [dbo].[Group]
        ADD CONSTRAINT [FK_dbo.Group_dbo.BinaryFile_PhotoId]
        FOREIGN KEY ( [PhotoId] ) REFERENCES [dbo].[BinaryFile] ( [Id] );
END" );
        }
    }
}
