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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Model.Groups.Group.Options;
using Rock.Security;
using Rock.ViewModels.Blocks;
using Rock.ViewModels.Blocks.Group.GroupDetail;
using Rock.ViewModels.Utility;
using Rock.Web;
using Rock.Web.Cache;

using RelationshipStrength = Rock.Enums.Group.RelationshipStrength;

namespace Rock.Blocks.Group
{
    /// <summary>
    /// Displays the details of a particular group. Phase 1 ships the block
    /// shell, the redesigned pure-Vue view panel, the Audit Details modal,
    /// and the terminal actions (Delete, Archive, ArchiveWithChildren,
    /// Copy). Edit mode is a Phase 2 placeholder.
    /// </summary>
    [DisplayName( "Group Detail" )]
    [Category( "Groups" )]
    [Description( "Displays the details of the given group." )]
    [IconCssClass( "ti ti-users-group" )]
    [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    [GroupTypesField(
        "Group Types Include",
        Description = "Select group types to show in this block. Leave all unchecked to show all group types.",
        IsRequired = false,
        Order = 0,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.GroupTypes )]

    [GroupTypesField(
        "Group Types Exclude",
        Description = "Select group types to exclude from this block. Note that this setting is only effective if 'Group Types Include' has no specific group types selected.",
        IsRequired = false,
        Order = 1,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.GroupTypesExclude )]

    [BooleanField(
        "Limit to Security Role Groups",
        Description = "Any group can be flagged as a security group (even if their group type is not a security role).  Should this block be limited to only show security role groups and group types?",
        DefaultBooleanValue = false,
        Order = 2,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.LimittoSecurityRoleGroups )]

    [BooleanField(
        "Limit to Group Types that are shown in navigation",
        Description = "Limits the Group Type list to those that are configured to be 'shown in navigation'.",
        DefaultBooleanValue = false,
        Order = 3,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.LimitToShowInNavigationGroupTypes )]

    [DefinedValueField(
        "Map Style",
        Description = "The style of map to use when displaying maps for this block.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.MAP_STYLES,
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = Rock.SystemGuid.DefinedValue.MAP_STYLE_ROCK,
        Order = 4,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.MapStyle )]

    [BooleanField(
        "Show Copy Button",
        Description = "Set this to true to show a Copy button at the bottom of the panel that will allow you to copy this group (with options for copying child groups).",
        DefaultBooleanValue = false,
        Order = 5,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.ShowCopyButton )]

    [BooleanField(
        "Show Location Addresses",
        Description = "Determines if the location address should be shown when viewing the group details.",
        DefaultBooleanValue = true,
        Order = 6,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.ShowLocationAddresses )]

    [BooleanField(
        "Prevent Selecting Inactive Campus",
        Description = "Should inactive campuses be excluded from the campus field when editing a group?",
        DefaultBooleanValue = false,
        Order = 7,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.PreventSelectingInactiveCampus )]

    [BooleanField(
        "Enable Group Tags",
        Description = "If enabled, the tags will be shown.",
        DefaultBooleanValue = true,
        Order = 8,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.EnableGroupTags )]

    [BooleanField(
        "Add Administrate Security to Group Creator",
        Description = "If enabled, the person who created the group will be granted 'Administrate' security rights to the group.  This was the default behavior in previous versions of Rock.  If disabled, the group creator will not be granted 'Administrate' security rights to the group.",
        DefaultBooleanValue = false,
        Order = 9,
        Category = AttributeCategory.GeneralSettings,
        Key = AttributeKey.AddAdministrateSecurityToGroupCreator )]

    [LinkedPage(
        "Group Map Page",
        Description = "The page to display detailed group map.",
        IsRequired = false,
        Order = 10,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupMapPage )]

    [LinkedPage(
        "Attendance Page",
        Description = "The page to display attendance list.",
        IsRequired = false,
        Order = 11,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.AttendancePage )]

    [LinkedPage(
        "Registration Instance Page",
        Description = "The page to display registration details.",
        IsRequired = false,
        Order = 12,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.RegistrationInstancePage )]

    [LinkedPage(
        "Event Item Occurrence Page",
        Description = "The page to display event item occurrence details.",
        IsRequired = false,
        Order = 13,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.EventItemOccurrencePage )]

    [LinkedPage(
        "Content Item Page",
        Description = "The page to display registration details.",
        IsRequired = false,
        Order = 14,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.ContentItemPage )]

    [LinkedPage(
        "Group List Page",
        Description = "The page to display related Group List.",
        IsRequired = false,
        Order = 15,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupListPage )]

    [LinkedPage(
        "Fundraising Progress Page",
        Description = "The page to display fundraising progress for all its members.",
        IsRequired = false,
        Order = 16,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.FundraisingProgressPage )]

    [LinkedPage(
        "Group History Page",
        Description = "The page to display group history.",
        IsRequired = false,
        Order = 17,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupHistoryPage )]

    [LinkedPage(
        "Group Scheduler Page",
        Description = "The page to schedule the group's resources.",
        DefaultValue = "1815D8C6-7C4A-4C05-A810-CF23BA937477,D0F198E2-6111-4EC1-8D1D-55AC10E28D04",
        IsRequired = false,
        Order = 18,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupSchedulerPage )]

    [LinkedPage(
        "Group RSVP List Page",
        Description = "The page to manage RSVPs for the group.",
        DefaultValue = Rock.SystemGuid.Page.GROUP_RSVP_LIST,
        IsRequired = false,
        Order = 19,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupRSVPPage )]

    [LinkedPage(
        "Group Placement Page",
        Description = "The page used for managing group placements.",
        IsRequired = false,
        Order = 20,
        Category = AttributeCategory.PageRouting,
        Key = AttributeKey.GroupPlacementPage )]

    #endregion Block Attributes

    [Rock.SystemGuid.EntityTypeGuid( "A0F8323D-B1A3-4DED-A6F6-B2483C0917D2" )]
    // was [Rock.SystemGuid.BlockTypeGuid( "87EEE2AD-F876-4827-9327-280FB1FDA1D1" )]
    [Rock.SystemGuid.BlockTypeGuid( "582BEEA1-5B27-444D-BC0A-F60CEB053981" )]
    public class GroupDetail : RockEntityDetailBlockType<Model.Group, GroupBag>, IBreadCrumbBlock
    {
        #region Keys

        private static class PageParameterKey
        {
            public const string GroupId = "GroupId";
            public const string ParentGroupId = "ParentGroupId";
            public const string ExpandedIds = "ExpandedIds";
            public const string ReturnUrl = "returnUrl";
        }

        private static class NavigationUrlKey
        {
            public const string AttendancePage = "AttendancePage";
            public const string GroupSchedulerPage = "GroupSchedulerPage";
            public const string GroupRSVPPage = "GroupRSVPPage";
            public const string GroupPlacementPage = "GroupPlacementPage";
            public const string GroupMapPage = "GroupMapPage";
            public const string GroupHistoryPage = "GroupHistoryPage";
            public const string FundraisingProgressPage = "FundraisingProgressPage";
            public const string RegistrationInstancePage = "RegistrationInstancePage";
            public const string EventItemOccurrencePage = "EventItemOccurrencePage";
            public const string ContentItemPage = "ContentItemPage";
            public const string GroupListPage = "GroupListPage";
        }

        private static class AttributeKey
        {
            public const string GroupTypes = "GroupTypes";
            public const string GroupTypesExclude = "GroupTypesExclude";
            public const string LimittoSecurityRoleGroups = "LimittoSecurityRoleGroups";
            public const string LimitToShowInNavigationGroupTypes = "LimitToShowInNavigationGroupTypes";
            public const string MapStyle = "MapStyle";
            public const string GroupMapPage = "GroupMapPage";
            public const string AttendancePage = "AttendancePage";
            public const string RegistrationInstancePage = "RegistrationInstancePage";
            public const string EventItemOccurrencePage = "EventItemOccurrencePage";
            public const string ContentItemPage = "ContentItemPage";
            public const string ShowCopyButton = "ShowCopyButton";
            public const string GroupListPage = "GroupListPage";
            public const string FundraisingProgressPage = "FundraisingProgressPage";
            public const string ShowLocationAddresses = "ShowLocationAddresses";
            public const string PreventSelectingInactiveCampus = "PreventSelectingInactiveCampus";
            public const string GroupHistoryPage = "GroupHistoryPage";
            public const string GroupSchedulerPage = "GroupSchedulerPage";
            public const string GroupRSVPPage = "GroupRSVPPage";
            public const string EnableGroupTags = "EnableGroupTags";
            public const string AddAdministrateSecurityToGroupCreator = "AddAdministrateSecurityToGroupCreator";
            public const string GroupPlacementPage = "GroupPlacementPage";
        }

        private static class AttributeCategory
        {
            public const string GeneralSettings = "General Settings";
            public const string PageRouting = "Page Routing";
        }

        /// <summary>
        /// Group attribute keys whose values surface in the redesigned
        /// Overview card. Each maps to a labeled row.
        /// </summary>
        private static class GroupAttributeKey
        {
            public const string Goal = "Goal";
            public const string Neighborhood = "Neighborhood";
            public const string Privacy = "Privacy";
            public const string GroupPreference = "GroupPreference";
        }

        #endregion Keys

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new DetailBlockBox<GroupBag, GroupDetailOptionsBag>();
            var entity = GetInitialEntity();

            SetBoxInitialEntityState( box, entity );

            box.NavigationUrls = GetBoxNavigationUrls();
            box.Options = GetBoxOptions( entity, box.NavigationUrls );

            return box;
        }

        /// <inheritdoc/>
        protected override Model.Group GetInitialEntity()
        {
            return GetInitialEntity<Model.Group, GroupService>( RockContext, PageParameterKey.GroupId );
        }

        /// <summary>
        /// Sets the initial entity state on the box. Mirrors the standard
        /// shape used by RockEntityDetailBlockType blocks: gate visibility
        /// on VIEW/EDIT auth and either populate
        /// <see cref="DetailBlockBox{TBag, TOptions}.Entity"/> or set
        /// <see cref="DetailBlockBox{TBag, TOptions}.ErrorMessage"/>.
        /// </summary>
        private void SetBoxInitialEntityState( DetailBlockBox<GroupBag, GroupDetailOptionsBag> box, Model.Group entity )
        {
            if ( entity == null )
            {
                box.ErrorMessage = $"The {Model.Group.FriendlyTypeName} was not found.";
                return;
            }

            var isViewable = entity.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson );
            box.IsEditable = entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );

            if ( entity.Id != 0 )
            {
                if ( isViewable )
                {
                    box.Entity = GetEntityBagForView( entity );
                }
                else
                {
                    box.ErrorMessage = EditModeMessage.NotAuthorizedToView( Model.Group.FriendlyTypeName );
                }
            }
            else
            {
                // Phase 1 only ships view mode for existing groups. Add via
                // the URL-with-GroupId-zero path is Phase 2 scope; the
                // placeholder edit panel renders an explanatory message.
                if ( box.IsEditable )
                {
                    box.Entity = GetEntityBagForEdit( entity );
                }
                else
                {
                    box.ErrorMessage = EditModeMessage.NotAuthorizedToEdit( Model.Group.FriendlyTypeName );
                }
            }

            PrepareDetailBox( box, entity );
        }

        /// <summary>
        /// Builds the per-block <see cref="GroupDetailOptionsBag"/> for
        /// the active group: the visibility flags driving the Group Tools
        /// card and the action-button visibility helpers. Outbound URLs
        /// themselves live on <c>DetailBlockBox.NavigationUrls</c>; this
        /// method consults that dictionary only to know whether a given
        /// LinkedPage attribute is configured.
        /// </summary>
        private GroupDetailOptionsBag GetBoxOptions( Model.Group entity, Dictionary<string, string> navigationUrls )
        {
            var options = new GroupDetailOptionsBag();

            var groupType = GetGroupTypeCache( entity );

            if ( entity == null || entity.Id == 0 || groupType == null )
            {
                return options;
            }

            // Group Tools visibility.
            bool IsToolVisible( string urlKey, bool gateCondition )
                => navigationUrls.TryGetValue( urlKey, out var url )
                    && url.IsNotNullOrWhiteSpace()
                    && gateCondition;

            options.IsAttendanceVisible  = IsToolVisible( NavigationUrlKey.AttendancePage,          groupType.TakesAttendance );
            options.IsSchedulerVisible   = IsToolVisible( NavigationUrlKey.GroupSchedulerPage,      groupType.IsSchedulingEnabled );
            options.IsRsvpVisible        = IsToolVisible( NavigationUrlKey.GroupRSVPPage,           groupType.EnableRSVP );
            options.IsPlacementVisible   = IsToolVisible( NavigationUrlKey.GroupPlacementPage,      true );
            options.IsMapVisible         = IsToolVisible( NavigationUrlKey.GroupMapPage,            true );
            options.IsHistoryVisible     = IsToolVisible( NavigationUrlKey.GroupHistoryPage,        groupType.EnableGroupHistory );
            options.IsFundraisingVisible = IsToolVisible( NavigationUrlKey.FundraisingProgressPage, IsFundraisingGroupType( entity.GroupTypeId ) );

            var canEdit = entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );
            options.IsCopyButtonShown = GetAttributeValue( AttributeKey.ShowCopyButton ).AsBoolean() && canEdit;

            var hasHistory = groupType.EnableGroupHistory
                && (
                    new GroupHistoricalService( RockContext ).Queryable().Any( a => a.GroupId == entity.Id )
                    || new GroupMemberHistoricalService( RockContext ).Queryable().Any( a => a.GroupId == entity.Id )
                );

            // Archive replaces Delete when the group type has history enabled and at least one
            // history row exists for the group
            options.ShouldShowArchiveInsteadOfDelete = hasHistory;
            options.IsArchiveVisible = !entity.IsSystem && !entity.IsArchived && canEdit && hasHistory;
            options.IsDeleteVisible  = !entity.IsSystem && !entity.IsArchived && canEdit && !hasHistory;

            options.IsTagListShown = GetAttributeValue( AttributeKey.EnableGroupTags ).AsBoolean() && groupType.EnableGroupTag;

            return options;
        }

        /// <summary>
        /// Builds the read-only / view-mode bag fields that are common
        /// across both view and edit modes for Phase 1. Phase 2 extends
        /// this method with edit-mode scalar fields.
        /// </summary>
        private GroupBag GetCommonEntityBag( Model.Group entity )
        {
            if ( entity == null )
            {
                return null;
            }

            var groupType = GetGroupTypeCache( entity );
            var canEdit = entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );
            var canAdministrate = entity.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson );

            var hasChildGroups = entity.Id > 0 && new GroupService( RockContext ).Queryable().Any( g => g.ParentGroupId == entity.Id );

            return new GroupBag
            {
                IdKey = entity.IdKey,
                IsActive = entity.IsActive,
                IsSystem = entity.IsSystem,
                IsArchived = entity.IsArchived,
                CanEdit = canEdit,
                CanAdministrate = canAdministrate,
                HasChildGroups = hasChildGroups,

                // Header chrome.
                Name = entity.Name,
                IconCssClass = groupType?.IconCssClass,
                GroupType = BuildGroupTypeRef( entity.GroupType ),
                CampusName = entity.CampusId.HasValue ? CampusCache.Get( entity.CampusId.Value )?.Name : null,

                // Subheader.
                IsPublic = entity.IsPublic,
                RelationshipStrength = GetRelationshipStrength( entity, groupType ),

                // Overview body. PhotoUrl stays null in Phase 1 because
                // Group has no photo column today; Phase 2 ships the
                // Group.PhotoId migration per 00-architecture.md Q8.
                PhotoUrl = null,
                Description = entity.Description,
                Administrator = BuildAdministratorRef( entity.GroupAdministratorPersonAlias?.Person, groupType ),
                ParentGroup = BuildParentGroupRef( entity.ParentGroup ),
                ScheduleFriendlyText = entity.Schedule?.FriendlyScheduleText,
                GroupCapacity = entity.GroupCapacity,
                GroupGoal = GetGroupAttributeValue( entity, GroupAttributeKey.Goal ),
                Neighborhood = GetGroupAttributeValue( entity, GroupAttributeKey.Neighborhood ),
                Privacy = GetGroupAttributeValue( entity, GroupAttributeKey.Privacy ),
                GroupPreference = GetGroupAttributeValue( entity, GroupAttributeKey.GroupPreference )
            };
        }

        /// <inheritdoc/>
        protected override GroupBag GetEntityBagForView( Model.Group entity )
        {
            if ( entity == null )
            {
                return null;
            }

            var bag = GetCommonEntityBag( entity );
            bag.Linkages = BuildLinkages( entity );
            return bag;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Phase 1 placeholder. The Edit block action returns a minimal
        /// bag plus the security-grant token; the full edit-mode bag
        /// arrives in Phase 2. The Vue layer renders an explanatory
        /// "edit panel coming in Phase 2" message during the gap.
        /// </remarks>
        protected override GroupBag GetEntityBagForEdit( Model.Group entity )
        {
            return GetCommonEntityBag( entity );
        }

        /// <inheritdoc/>
        protected override bool TryGetEntityForEditAction( string idKey, out Model.Group entity, out BlockActionResult error )
        {
            var entityService = new GroupService( RockContext );
            error = null;

            if ( idKey.IsNotNullOrWhiteSpace() )
            {
                entity = entityService.Get( idKey, !PageCache.Layout.Site.DisablePredictableIds );
            }
            else
            {
                entity = new Model.Group();
                entityService.Add( entity );
            }

            if ( entity == null )
            {
                error = ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
                return false;
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                error = ActionBadRequest( $"Not authorized to edit {Model.Group.FriendlyTypeName}." );
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Phase 1 ships no Save body. The full Save flow lands in Phase
        /// 2 alongside the edit-panel scalar fields. Returning false here
        /// would have <c>Save</c> abort if it were ever invoked, but
        /// Phase 1 does not register a Save block action either.
        /// </remarks>
        protected override bool UpdateEntityFromBox( Model.Group entity, ValidPropertiesBox<GroupBag> box )
        {
            return false;
        }

        /// <inheritdoc/>
        public BreadCrumbResult GetBreadCrumbs( PageReference pageReference )
        {
            /*
                5/6/2026 - CLAUDE

                The WebForms block at GroupDetail.ascx.cs:592-615 returned
                "New Group" both for ?GroupId=0 (real Add) and for any
                ?GroupId=N where the group could not be loaded (deleted /
                stale Id). The latter is misleading: a user navigating to
                a deleted group's URL would see a breadcrumb implying they
                were creating something. Phase 1 fixes this so the Add
                crumb is only shown when the page parameter is actually
                an Add sentinel (integer 0 or empty Guid); a stale Id
                returns no crumb (same as the no-parameter branch).

                Reason: lookup-miss should not be rendered as Add.
            */

            var key = pageReference.GetPageParameter( PageParameterKey.GroupId );

            if ( key.IsNullOrWhiteSpace() )
            {
                return new BreadCrumbResult { BreadCrumbs = new List<IBreadCrumb>() };
            }

            var id = key.AsIntegerOrNull();
            var guid = key.AsGuidOrNull();
            var isAddPath = ( id.HasValue && id.Value == 0 )
                || ( guid.HasValue && guid.Value == Guid.Empty );

            if ( isAddPath )
            {
                var addCrumb = new BreadCrumbLink( "New Group", new PageReference( pageReference.PageId, 0 ) );
                return new BreadCrumbResult { BreadCrumbs = new List<IBreadCrumb> { addCrumb } };
            }

            // Per Q4 (00-architecture.md), outbound GroupId parameters
            // must be normalized to IdKey regardless of the inbound form.
            // Project to Id (a real SQL column) and hash to IdKey in C#
            // memory; Entity<T>.IdKey is [NotMapped] and cannot be
            // translated to SQL by GetSelect.
            var info = new GroupService( RockContext ).GetSelect( key, g => new { g.Name, g.Id } );
            if ( info == null )
            {
                return new BreadCrumbResult { BreadCrumbs = new List<IBreadCrumb>() };
            }

            var pageParameters = new Dictionary<string, string>
            {
                [PageParameterKey.GroupId] = Rock.Utility.IdHasher.Instance.GetHash( info.Id )
            };
            var breadCrumbPageRef = new PageReference( pageReference.PageId, 0, pageParameters );
            var breadCrumb = new BreadCrumbLink( info.Name, breadCrumbPageRef );

            return new BreadCrumbResult
            {
                BreadCrumbs = new List<IBreadCrumb> { breadCrumb }
            };
        }

        /// <summary>
        /// Builds the navigation URL dictionary for the block. Outbound
        /// URLs are emitted with the <c>((Key))</c> placeholder so the
        /// Vue layer can substitute the active group's IdKey at render
        /// time per Q4. The 5 still-WebForms destinations (GroupListPage,
        /// FundraisingProgressPage, GroupHistoryPage, GroupMapPage,
        /// GroupSchedulerPage) will produce broken links until Phase 7
        /// updates them to accept IdKey - acknowledged tradeoff.
        /// </summary>
        private Dictionary<string, string> GetBoxNavigationUrls()
        {
            var groupIdParam = new Dictionary<string, string>
            {
                [PageParameterKey.GroupId] = "((Key))"
            };

            return new Dictionary<string, string>
            {
                [NavigationUrlKey.AttendancePage]           = this.GetLinkedPageUrl( AttributeKey.AttendancePage, groupIdParam ),
                [NavigationUrlKey.GroupSchedulerPage]       = this.GetLinkedPageUrl( AttributeKey.GroupSchedulerPage, groupIdParam ),
                [NavigationUrlKey.GroupRSVPPage]            = this.GetLinkedPageUrl( AttributeKey.GroupRSVPPage, groupIdParam ),
                [NavigationUrlKey.GroupPlacementPage]       = this.GetLinkedPageUrl( AttributeKey.GroupPlacementPage, groupIdParam ),
                [NavigationUrlKey.GroupMapPage]             = this.GetLinkedPageUrl( AttributeKey.GroupMapPage, groupIdParam ),
                [NavigationUrlKey.GroupHistoryPage]         = this.GetLinkedPageUrl( AttributeKey.GroupHistoryPage, groupIdParam ),
                [NavigationUrlKey.FundraisingProgressPage]  = this.GetLinkedPageUrl( AttributeKey.FundraisingProgressPage, groupIdParam ),
                [NavigationUrlKey.RegistrationInstancePage] = this.GetLinkedPageUrl( AttributeKey.RegistrationInstancePage, groupIdParam ),
                [NavigationUrlKey.EventItemOccurrencePage]  = this.GetLinkedPageUrl( AttributeKey.EventItemOccurrencePage, groupIdParam ),
                [NavigationUrlKey.ContentItemPage]          = this.GetLinkedPageUrl( AttributeKey.ContentItemPage, groupIdParam ),
                [NavigationUrlKey.GroupListPage]            = this.GetLinkedPageUrl( AttributeKey.GroupListPage, groupIdParam ),
            };
        }

        #endregion Methods

        #region Block Actions

        /// <summary>
        /// Returns the bag for entering edit mode. Phase 1 returns the
        /// same view-mode bag plus the security grant token; the full
        /// edit-mode bag (scalar fields, GroupType cascade, peer-network
        /// overrides, RSVP / Scheduling / Chat sections) lands in Phase 2.
        /// </summary>
        [BlockAction]
        public BlockActionResult Edit( string key )
        {
            if ( !TryGetEntityForEditAction( key, out var entity, out var actionError ) )
            {
                return actionError;
            }

            var bag = GetEntityBagForEdit( entity );

            return ActionOk( new ValidPropertiesBox<GroupBag>
            {
                Bag = bag,
                ValidProperties = new List<string>()
            } );
        }

        /// <summary>
        /// Deletes the specified group. Mirrors the WebForms parity at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L675"/>:
        /// EDIT auth, <c>CanDelete</c> validation, inline-schedule
        /// cleanup, and the <c>DeleteSecurityRoleGroup</c> branch for
        /// security-role groups.
        /// </summary>
        [BlockAction]
        public BlockActionResult Delete( string key )
        {
            var groupService = new GroupService( RockContext );
            var entity = groupService.Get( key, !PageCache.Layout.Site.DisablePredictableIds );

            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to delete this group." );
            }

            if ( !groupService.CanDelete( entity, out var errorMessage, includeSecondLvl: true ) )
            {
                return ActionBadRequest( errorMessage );
            }

            var parentGroupId = entity.ParentGroupId;

            // If the group has a non-named (inline) schedule and no other
            // group is using it, delete the schedule too. Mirrors the
            // WebForms cleanup at GroupDetail.ascx.cs:700-713.
            if ( entity.ScheduleId.HasValue )
            {
                var scheduleService = new ScheduleService( RockContext );
                var schedule = scheduleService.Get( entity.ScheduleId.Value );
                if ( schedule != null && schedule.ScheduleType != ScheduleType.Named )
                {
                    var isReferencedByOtherGroup = groupService.Queryable()
                        .Any( g => g.ScheduleId == schedule.Id && g.Id != entity.Id );
                    if ( !isReferencedByOtherGroup )
                    {
                        scheduleService.Delete( schedule );
                    }
                }
            }

            // Security-role groups need the dedicated cleanup path so the
            // related Auth rows and global cache invalidation happen
            // correctly. Non-security groups go through the standard
            // Service.Delete flow.
            if ( entity.IsSecurityRoleOrSecurityGroupType() )
            {
                GroupService.DeleteSecurityRoleGroup( entity.Id );
            }
            else
            {
                groupService.Delete( entity );
            }

            RockContext.SaveChanges();

            return ActionOk( NavigateAfterDeleteOrArchive( parentGroupId ) );
        }

        /// <summary>
        /// Archives the specified group only (no descendant cascade).
        /// The Vue layer reads <see cref="GroupBag.HasChildGroups"/> and
        /// prompts the user before deciding whether to call this action
        /// or <see cref="ArchiveWithChildren(string)"/>. Mirrors the
        /// WebForms <c>ArchiveSingleGroup</c> worker at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3378"/>.
        /// </summary>
        [BlockAction]
        public BlockActionResult Archive( string key )
        {
            var groupService = new GroupService( RockContext );
            var entity = groupService.Get( key, !PageCache.Layout.Site.DisablePredictableIds );

            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to archive this group." );
            }

            var personAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;

            groupService.Archive( entity, personAliasId, true );
            RockContext.SaveChanges();

            return ActionOk( NavigateAfterDeleteOrArchive( entity.ParentGroupId ) );
        }

        /// <summary>
        /// Archives the specified group plus every descendant group via
        /// <c>GetAllDescendentGroups</c>. Mirrors the WebForms parity at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L3407"/>.
        /// </summary>
        [BlockAction]
        public BlockActionResult ArchiveWithChildren( string key )
        {
            var groupService = new GroupService( RockContext );
            var entity = groupService.Get( key, !PageCache.Layout.Site.DisablePredictableIds );

            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to archive this group." );
            }

            var personAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;
            var descendants = groupService.GetAllDescendentGroups( entity.Id, true );
            foreach ( var descendant in descendants )
            {
                groupService.Archive( descendant, personAliasId, true );
            }

            groupService.Archive( entity, personAliasId, true );
            RockContext.SaveChanges();

            return ActionOk( NavigateAfterDeleteOrArchive( entity.ParentGroupId ) );
        }

        /// <summary>
        /// Copies the specified group via <see cref="GroupService.CopyGroup"/>.
        /// The Vue modal defaults <c>IncludeChildGroups</c> to <c>false</c>,
        /// flipped from the WebForms default per design behavior change C1.
        /// </summary>
        [BlockAction]
        public BlockActionResult Copy( CopyGroupRequestBag bag )
        {
            if ( bag == null || bag.Key.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "Invalid request." );
            }

            var groupService = new GroupService( RockContext );
            var entity = groupService.Get( bag.Key, !PageCache.Layout.Site.DisablePredictableIds );

            if ( entity == null )
            {
                return ActionBadRequest( $"{Model.Group.FriendlyTypeName} not found." );
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to copy the group." );
            }

            var copyOptions = new CopyGroupOptions
            {
                GroupId = entity.Id,
                IncludeChildGroups = bag.IncludeChildGroups,
                CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
            };

            var newGroupId = GroupService.CopyGroup( copyOptions );

            // Copy navigation does NOT honor returnUrl per the WebForms
            // parity at GroupDetail.ascx.cs:1517-1545. Always reload to
            // the new group.
            var newGroupKey = newGroupId.HasValue && newGroupId.Value > 0
                ? Rock.Utility.IdHasher.Instance.GetHash( newGroupId.Value )
                : entity.IdKey;

            var qryParams = new Dictionary<string, string>
            {
                [PageParameterKey.GroupId] = newGroupKey
            };

            var expandedIds = PageParameter( PageParameterKey.ExpandedIds );
            if ( expandedIds.IsNotNullOrWhiteSpace() )
            {
                qryParams[PageParameterKey.ExpandedIds] = expandedIds;
            }

            return ActionOk( this.GetCurrentPageUrl( qryParams ) );
        }

        #endregion Block Actions

        #region Helper Methods

        /// <summary>
        /// Resolves the cached <see cref="GroupTypeCache"/> for the supplied
        /// entity, returning null when the entity is null or has no
        /// resolvable <c>GroupTypeId</c>. Centralizes the lookup so callers
        /// don't repeat the null / zero-Id guards. Callers that need to
        /// further restrict to saved groups (e.g., features that don't
        /// apply during Add mode) should layer an <c>entity.Id &gt; 0</c>
        /// check on top.
        /// </summary>
        private static GroupTypeCache GetGroupTypeCache( Model.Group entity )
        {
            if ( entity == null || entity.GroupTypeId <= 0 )
            {
                return null;
            }

            return GroupTypeCache.Get( entity.GroupTypeId );
        }

        /// <summary>
        /// Resolves the effective <see cref="RelationshipStrength"/> for
        /// the subheader chip: the group's override falling back to the
        /// group type default. Returns null when the group type lacks
        /// peer-network support, hiding the chip. Non-canonical stored
        /// integers round-trip as-is; the Vue side's
        /// <c>RelationshipStrengthDescription</c> lookup returns
        /// undefined for those, which also hides the chip.
        /// </summary>
        private static RelationshipStrength? GetRelationshipStrength( Model.Group entity, GroupTypeCache groupType )
        {
            return groupType?.IsPeerNetworkEnabled == true
                ? ( RelationshipStrength? )( entity.RelationshipStrengthOverride ?? groupType.RelationshipStrength )
                : null;
        }

        /// <summary>
        /// Returns true when the supplied group type Id is the
        /// <c>FundraisingOpportunity</c> group type or its immediate
        /// inheritor via <c>InheritedGroupTypeId</c>. Mirrors the WebForms
        /// visibility check at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2827"/>,
        /// which only checks one level of inheritance.
        /// </summary>
        private static bool IsFundraisingGroupType( int groupTypeId )
        {
            var fundraisingGroupTypeId = GroupTypeCache.GetId( Rock.SystemGuid.GroupType.GROUPTYPE_FUNDRAISINGOPPORTUNITY.AsGuid() );
            if ( !fundraisingGroupTypeId.HasValue )
            {
                return false;
            }

            var groupType = GroupTypeCache.Get( groupTypeId );
            if ( groupType == null )
            {
                return false;
            }

            return groupType.Id == fundraisingGroupTypeId.Value
                || groupType.InheritedGroupTypeId == fundraisingGroupTypeId.Value;
        }

        /// <summary>
        /// Reads a group attribute value by key, loading attributes if
        /// they have not been loaded yet. Used to surface the Overview
        /// card's four attribute-driven rows (Goal / Neighborhood /
        /// Privacy / Group Preference) without coupling them to the edit
        /// flow.
        /// </summary>
        private string GetGroupAttributeValue( Model.Group entity, string key )
        {
            if ( entity == null )
            {
                return null;
            }

            if ( entity.Attributes == null )
            {
                entity.LoadAttributes( RockContext );
            }

            return entity.GetAttributeValue( key );
        }

        /// <summary>
        /// Builds the administrator reference for the Overview card.
        /// Returns null when the person is missing OR when the group
        /// type's <c>ShowAdministrator</c> flag is false (which hides
        /// the row). The link URL is server-resolved via
        /// <see cref="ResolveEntityUrl"/> so any customer-customized
        /// Person <c>LinkUrlLavaTemplate</c> is honored; the fallback is
        /// <c>/Person/{IdKey}</c>.
        /// </summary>
        private static GroupAdministratorBag BuildAdministratorRef( Person person, GroupTypeCache groupType )
        {
            if ( person == null || groupType == null || !groupType.ShowAdministrator )
            {
                return null;
            }

            return new GroupAdministratorBag
            {
                Name = person.FullName,
                Url = ResolveEntityUrl( typeof( Person ), person, fallbackUrl: $"/Person/{person.IdKey}" )
            };
        }

        /// <summary>
        /// Builds the parent group reference for the Overview card.
        /// Returns null when the group has no parent (the consuming row
        /// hides). The link URL is server-resolved via
        /// <see cref="ResolveEntityUrl"/> so any customer-customized
        /// Group <c>LinkUrlLavaTemplate</c> is honored; the fallback is
        /// <c>/Group/{IdKey}</c>.
        /// </summary>
        private static ParentGroupBag BuildParentGroupRef( Model.Group parentGroup )
        {
            if ( parentGroup == null )
            {
                return null;
            }

            return new ParentGroupBag
            {
                Name = parentGroup.Name,
                Url = ResolveEntityUrl( typeof( Model.Group ), parentGroup, fallbackUrl: $"/Group/{parentGroup.IdKey}" )
            };
        }

        /// <summary>
        /// Builds the group type reference for the panel-header chip.
        /// Returns null when no group type is supplied. <c>Url</c> is
        /// populated only when the current user has ADMINISTRATE on the
        /// group type (otherwise the chip renders as plain text). The URL
        /// is server-resolved via
        /// <see cref="ResolveEntityUrl(System.Type, object, string)"/>
        /// so any customer-customized GroupType <c>LinkUrlLavaTemplate</c>
        /// is honored; the fallback is the
        /// <c>/page/GroupTypeDetail?GroupTypeId={IdKey}</c> admin route.
        /// </summary>
        private GroupDetailGroupTypeBag BuildGroupTypeRef( Model.GroupType groupType )
        {
            if ( groupType == null )
            {
                return null;
            }

            var canAdministrate = groupType.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson );

            return new GroupDetailGroupTypeBag
            {
                Name = groupType.Name,
                Url = canAdministrate
                    ? ResolveEntityUrl(
                        typeof( Model.GroupType ),
                        groupType,
                        fallbackUrl: $"/page/GroupTypeDetail?GroupTypeId={Rock.Utility.IdHasher.Instance.GetHash( groupType.Id )}" )
                    : null
            };
        }

        /// <summary>
        /// Resolves a server-side detail URL for the supplied entity.
        /// Prefers the entity type's <c>LinkUrlLavaTemplate</c> (so
        /// customer-customized link rules are honored) with
        /// <paramref name="lavaEntity"/> merged in as <c>Entity</c>;
        /// rebases <c>~/</c> against the <c>InternalApplicationRoot</c>
        /// global attribute. Falls back to <paramref name="fallbackUrl"/>
        /// when the template is null / empty or resolves to whitespace.
        /// </summary>
        private static string ResolveEntityUrl( System.Type entityType, object lavaEntity, string fallbackUrl )
        {
            if ( lavaEntity == null )
            {
                return null;
            }

            var entityTypeCache = EntityTypeCache.Get( entityType );
            if ( !string.IsNullOrWhiteSpace( entityTypeCache?.LinkUrlLavaTemplate ) )
            {
                var mergeFields = new Dictionary<string, object>
                {
                    ["Entity"] = lavaEntity
                };

                var url = entityTypeCache.LinkUrlLavaTemplate.ResolveMergeFields( mergeFields );
                if ( !string.IsNullOrWhiteSpace( url ) )
                {
                    if ( url.StartsWith( "~/" ) )
                    {
                        var baseUrl = GlobalAttributesCache.Value( "InternalApplicationRoot" );
                        url = url.Replace( "~/", baseUrl.EnsureTrailingForwardslash() );
                    }
                    return url;
                }
            }

            return fallbackUrl;
        }

        /// <summary>
        /// Builds the Linkages payload for the Overview card. Returns
        /// null when the group has no linkages of any kind, which omits
        /// the entire section in the view.
        /// </summary>
        /// <remarks>
        /// 5/6/2026 - CLAUDE
        ///
        /// The Phase 1 spec checklist item 49 references
        /// <c>ContentChannelItemAssociation</c> but that table holds only
        /// parent/child relationships between two content items, not
        /// group-to-content-item associations. The actual link path is
        /// Group.Linkages → EventItemOccurrenceGroupMap → EventItemOccurrence
        /// → EventItemOccurrenceChannelItem → ContentChannelItem, which is
        /// what the WebForms default Lava template walks. Logged as a
        /// mid-phase decision.
        ///
        /// Reason: the spec entity reference was incorrect.
        /// </remarks>
        private GroupLinkagesBag BuildLinkages( Model.Group entity )
        {
            if ( entity == null || entity.Id == 0 )
            {
                return null;
            }

            // EventItemOccurrenceGroupMap rows are the basis for both
            // registrations and event item occurrences (and indirectly
            // content items via EventItemOccurrenceChannelItem). One query
            // surfaces all three.
            var linkageRows = new EventItemOccurrenceGroupMapService( RockContext ).Queryable()
                .AsNoTracking()
                .Include( l => l.RegistrationInstance )
                .Include( l => l.EventItemOccurrence )
                .Include( l => l.EventItemOccurrence.EventItem )
                .Include( l => l.EventItemOccurrence.ContentChannelItems.Select( c => c.ContentChannelItem ) )
                .Where( l => l.GroupId == entity.Id )
                .ToList();

            // Per Q4, every outbound Id-style page parameter is IdKey -
            // not only GroupId. Each linkage URL emits the receiving
            // entity's IdKey instead of its integer Id. The Obsidian
            // RegistrationInstanceDetail and the WebForms
            // EventItemOccurrenceDetail / ContentChannelItemDetail all
            // accept IdKey on their corresponding page parameters today
            // (verified during the Q4 audit).
            var registrations = linkageRows
                .Where( l => l.RegistrationInstance != null )
                .Select( l => l.RegistrationInstance )
                .GroupBy( r => r.Id )
                .Select( g => g.First() )
                .Select( r => new GroupLinkageBag
                {
                    Name = r.Name,
                    Url = this.GetLinkedPageUrl( AttributeKey.RegistrationInstancePage, "RegistrationInstanceId", r.IdKey )
                } )
                .Where( l => l.Url.IsNotNullOrWhiteSpace() )
                .ToList();

            var eventItemOccurrences = linkageRows
                .Where( l => l.EventItemOccurrence != null && l.EventItemOccurrence.EventItem != null )
                .Select( l => l.EventItemOccurrence )
                .GroupBy( e => e.Id )
                .Select( g => g.First() )
                .Select( e => new GroupLinkageBag
                {
                    Name = e.EventItem.Name,
                    Url = this.GetLinkedPageUrl( AttributeKey.EventItemOccurrencePage, "EventItemOccurrenceId", e.IdKey )
                } )
                .Where( l => l.Url.IsNotNullOrWhiteSpace() )
                .ToList();

            var contentItems = linkageRows
                .Where( l => l.EventItemOccurrence != null )
                .SelectMany( l => l.EventItemOccurrence.ContentChannelItems )
                .Select( c => c.ContentChannelItem )
                .Where( c => c != null )
                .GroupBy( c => c.Id )
                .Select( g => g.First() )
                .Select( c => new GroupLinkageBag
                {
                    Name = c.Title,
                    Url = this.GetLinkedPageUrl( AttributeKey.ContentItemPage, "ContentItemId", c.IdKey )
                } )
                .Where( l => l.Url.IsNotNullOrWhiteSpace() )
                .ToList();

            if ( !registrations.Any() && !eventItemOccurrences.Any() && !contentItems.Any() )
            {
                return null;
            }

            return new GroupLinkagesBag
            {
                Registrations = registrations,
                EventItemOccurrences = eventItemOccurrences,
                ContentItems = contentItems
            };
        }

        /// <summary>
        /// Validates a candidate <c>returnUrl</c> page parameter is
        /// same-origin before any redirect uses it. Treats null /
        /// whitespace as "no return URL" (caller falls through to default
        /// navigation) and accepts relative paths (no scheme, no host).
        /// Absolute URLs are accepted only when their host matches the
        /// current request host. Closes the L6 open-redirect risk per
        /// 00-architecture.md Q12.
        /// </summary>
        private bool IsSafeReturnUrl( string url )
        {
            if ( url.IsNullOrWhiteSpace() )
            {
                return true;
            }

            if ( !Uri.TryCreate( url, UriKind.RelativeOrAbsolute, out var parsedUri ) )
            {
                return false;
            }

            if ( !parsedUri.IsAbsoluteUri )
            {
                // Reject protocol-relative ("//attacker.example/...") URLs
                // that some parsers treat as relative.
                return !url.StartsWith( "//", StringComparison.Ordinal );
            }

            var requestHost = RequestContext?.RequestUri?.Host;
            return requestHost.IsNotNullOrWhiteSpace()
                && string.Equals( parsedUri.Host, requestHost, StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Computes the post-Delete / post-Archive redirect URL using the
        /// same precedence as the WebForms block at
        /// <see href="../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L735"/>:
        /// honor a same-origin <c>returnUrl</c> first; otherwise build a
        /// query string with the parent group's IdKey plus
        /// <c>ExpandedIds</c> and resolve <c>GroupListPage</c>; otherwise
        /// reload the current page.
        /// </summary>
        private string NavigateAfterDeleteOrArchive( int? parentGroupId )
        {
            var returnUrl = PageParameter( PageParameterKey.ReturnUrl );
            if ( returnUrl.IsNotNullOrWhiteSpace() && IsSafeReturnUrl( returnUrl ) )
            {
                return returnUrl;
            }

            var qryParams = new Dictionary<string, string>();
            if ( parentGroupId.HasValue && parentGroupId.Value > 0 )
            {
                qryParams[PageParameterKey.GroupId] = Rock.Utility.IdHasher.Instance.GetHash( parentGroupId.Value );
            }

            var expandedIds = PageParameter( PageParameterKey.ExpandedIds );
            if ( expandedIds.IsNotNullOrWhiteSpace() )
            {
                qryParams[PageParameterKey.ExpandedIds] = expandedIds;
            }

            if ( GetAttributeValue( AttributeKey.GroupListPage ).AsGuid() != Guid.Empty )
            {
                return this.GetLinkedPageUrl( AttributeKey.GroupListPage, qryParams );
            }

            return this.GetCurrentPageUrl( qryParams );
        }

        #endregion Helper Methods
    }
}
