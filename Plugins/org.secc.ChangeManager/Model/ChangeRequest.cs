using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using org.secc.ChangeManager.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.ChangeManager.Model
{
    [Table( "_org_secc_ChangeManager_ChangeRequest" )]
    [DataContract]
    public class ChangeRequest : Rock.Data.Model<ChangeRequest>, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        [DataMember]
        public string Name { get; set; }

        [Index]
        [DataMember]
        public int EntityTypeId { get; set; }

        public virtual EntityType EntityType { get; set; }

        [Index]
        [DataMember]
        public int EntityId { get; set; }

        [Index]
        [DataMember]
        public int RequestorAliasId { get; set; }

        [LavaInclude]
        public virtual PersonAlias RequestorAlias { get; set; }

        [Index]
        [DataMember]
        public int ApproverAliasId { get; set; }

        [LavaInclude]
        public virtual PersonAlias ApproverAlias { get; set; }

        [Index]
        [DataMember]
        public bool IsComplete { get; set; }

        [DataMember]
        public string RequestorComment { get; set; }

        [DataMember]
        public string ApproverComment { get; set; }

        [LavaInclude]
        public virtual ICollection<ChangeRecord> ChangeRecords
        {
            get { return _changeRecords ?? ( _changeRecords = new Collection<ChangeRecord>() ); }
            set { _changeRecords = value; }
        }
        private ICollection<ChangeRecord> _changeRecords;

        #region Overrides

        public override void PreSaveChanges( Rock.Data.DbContext dbContext, EntityState state )
        {
            base.PreSaveChanges( dbContext, state );
            base.PostSaveChanges( dbContext );

            var entityTypeCache = EntityTypeCache.Get( EntityTypeId );
            var entityType = entityTypeCache.GetEntityType();
            var entityService = Reflection.GetServiceForEntityType( entityType, dbContext );
            System.Reflection.MethodInfo getMethod = entityService.GetType().GetMethod( "Get", new Type[] { typeof( int ) } );
            var mergeObjectEntity = getMethod.Invoke( entityService, new object[] { EntityId } ) as Rock.Data.IEntity;

            this.Name = mergeObjectEntity != null ? mergeObjectEntity.ToString() : "Unknown Entity";
        }

        #endregion

        #region Methods

        public void CompleteChanges( out List<string> errors )
        {
            CompleteChanges( new RockContext(), out errors );
        }

        public void CompleteChanges( RockContext rockContext, out List<string> errors )
        {
            errors = new List<string>();

            SecurityChangeDetail securityChangeDetail = new SecurityChangeDetail
            {
                ChangeRequestId = this.Id,
                ChangeDetails = new List<string>()
            };

            using ( var dbContextTransaction = rockContext.Database.BeginTransaction() )
            {
                try
                {
                    IEntity entity = GetEntity( EntityTypeId, EntityId, rockContext, errors );

                    //Make changes
                    foreach ( var changeRecord in ChangeRecords.Where( r => r.WasApplied != true && r.IsRejected == false ) )
                    {
                        var targetEntity = entity;
                        if ( changeRecord.RelatedEntityTypeId.HasValue )
                        {
                            if ( changeRecord.RelatedEntityId.HasValue &&
                                !( changeRecord.RelatedEntityId == 0 || changeRecord.Action == ChangeRecordAction.Create ) )
                            {
                                //existing entity
                                targetEntity = GetEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.RelatedEntityId.Value, rockContext, errors );
                            }
                            else
                            {
                                //new entity
                                targetEntity = CreateNewEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.NewValue, rockContext, errors );
                                changeRecord.RelatedEntityId = targetEntity.Id;

                                //if we add a phone number add that to the change list
                                if ( targetEntity?.GetType() == typeof( PhoneNumber ) )
                                {
                                    var number = targetEntity as PhoneNumber;
                                    var numberType = DefinedValueCache.GetValue( number.NumberTypeValueId.Value );
                                    securityChangeDetail.ChangeDetails.Add( string.Format( "Added {0} phone number {1}", numberType, number.NumberFormatted ) );
                                }
                            }
                        }

                        //Some times entitities can be deleted (such as a binary file being cleaned up)
                        //And cannot update, delete or change and attribute on it anymore
                        if ( targetEntity == null )
                        {
                            continue;
                        }

                        //Remove records marked as delete
                        if ( changeRecord.Action == ChangeRecordAction.Delete )
                        {
                            DeleteEntity( targetEntity, rockContext, errors );
                        }
                        else if ( changeRecord.Action == ChangeRecordAction.Attribute )
                        {
                            UpdateAttribute( targetEntity, changeRecord.Property, changeRecord.NewValue, rockContext, errors );
                        }
                        else if ( changeRecord.Property.IsNotNullOrWhiteSpace() )
                        {
                            PropertyInfo prop = targetEntity.GetType().GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );

                            if ( prop != null && prop.PropertyType.GetInterfaces().Any( i => i.IsInterface && i.GetInterfaces().Contains( typeof( IEntity ) ) ) )
                            {
                                PropertyInfo propId = targetEntity.GetType().GetProperty( changeRecord.Property + "Id", BindingFlags.Public | BindingFlags.Instance );
                                var newObject = changeRecord.NewValue.FromJsonOrNull<BasicEntity>();
                                prop.SetValue( targetEntity, null, null );
                                if ( newObject != null )
                                {
                                    propId.SetValue( targetEntity, newObject.Id );
                                }
                                else
                                {
                                    propId.SetValue( targetEntity, null, null );
                                }
                            }
                            else
                            {
                                SetProperty( targetEntity, prop, changeRecord.NewValue, errors );
                            }
                        }
                        changeRecord.WasApplied = true;

                        //Check to see if the email address was changed
                        var targetEntityType = targetEntity?.GetType();
                        if ( targetEntityType?.BaseType == typeof( Person ) // is a person
                            && changeRecord.Property == "Email" // and changed email
                            && changeRecord.OldValue.IsNotNullOrWhiteSpace() //old value isn't blank
                            && changeRecord.NewValue.IsNotNullOrWhiteSpace() //new value isn't blank
                            )
                        {
                            securityChangeDetail.ChangeDetails.Add( string.Format( "Changed email address from {0} to {1}", changeRecord.OldValue, changeRecord.NewValue ) );
                            securityChangeDetail.CurrentEmail = changeRecord.NewValue;
                            securityChangeDetail.PreviousEmail = changeRecord.OldValue;
                        }

                        //Check to see if the phone number was changed
                        if ( targetEntityType?.BaseType == typeof( PhoneNumber ) //is a phonenuumber
                            && changeRecord.Property == "Number"  //is a number change
                            )
                        {
                            var number = targetEntity as PhoneNumber;
                            var numberType = DefinedValueCache.GetValue( number.NumberTypeValueId.Value );
                            securityChangeDetail.ChangeDetails.Add( string.Format( "Changed {0} phone number from {1} to {2}",
                                numberType,
                                PhoneNumber.FormattedNumber( PhoneNumber.DefaultCountryCode(), changeRecord.OldValue ),
                                PhoneNumber.FormattedNumber( PhoneNumber.DefaultCountryCode(), changeRecord.NewValue ) ) );
                        }
                    }

                    //Undo Changes
                    foreach ( var changeRecord in ChangeRecords.Where( r => r.WasApplied == true && r.IsRejected == true ) )
                    {
                        var targetEntity = entity;

                        if ( changeRecord.RelatedEntityTypeId.HasValue && changeRecord.Action != ChangeRecordAction.Delete )
                        {
                            if ( changeRecord.RelatedEntityId.HasValue && changeRecord.Action != ChangeRecordAction.Create )
                            {
                                //existing entity
                                targetEntity = GetEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.RelatedEntityId.Value, rockContext, errors );
                            }
                            else
                            {
                                //This was a created entity that we must now murder in cold blood.
                                targetEntity = GetEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.RelatedEntityId.Value, rockContext, errors );
                                DeleteEntity( targetEntity, rockContext, errors );
                                changeRecord.WasApplied = false;
                                continue;
                            }
                        }

                        //Undelete
                        if ( changeRecord.RelatedEntityTypeId.HasValue && changeRecord.Action == ChangeRecordAction.Delete )
                        {
                            targetEntity = CreateNewEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.OldValue, rockContext, errors );
                            changeRecord.RelatedEntityId = targetEntity.Id;
                        }
                        else if ( changeRecord.Action == ChangeRecordAction.Attribute )
                        {
                            UpdateAttribute( targetEntity, changeRecord.Property, changeRecord.OldValue, rockContext, errors );
                        }
                        //Property changes
                        else if ( changeRecord.Property.IsNotNullOrWhiteSpace() )
                        {
                            PropertyInfo prop = targetEntity?.GetType()?.GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );
                            if ( targetEntity == null || prop == null )
                            {
                                //Entity was probably deleted after the change request
                                continue;
                            }

                            if ( prop.PropertyType.GetInterfaces().Any( i => i.IsInterface && i.GetInterfaces().Contains( typeof( IEntity ) ) ) )
                            {
                                PropertyInfo propId = targetEntity.GetType().GetProperty( changeRecord.Property + "Id", BindingFlags.Public | BindingFlags.Instance );
                                var oldObject = changeRecord.OldValue.FromJsonOrNull<BasicEntity>();
                                prop.SetValue( targetEntity, null, null );
                                if ( oldObject != null )
                                {
                                    propId.SetValue( targetEntity, oldObject.Id );
                                }
                                else
                                {
                                    propId.SetValue( targetEntity, null, null );
                                }
                            }
                            else
                            {
                                SetProperty( targetEntity, prop, changeRecord.OldValue, errors );
                            }
                        }
                        changeRecord.WasApplied = false;
                    }


                    try
                    {
                        rockContext.SaveChanges();
                    }
                    catch ( Exception e )
                    {
                        errors.AddRange( e.Messages() );
                    }

                    if ( errors.Any() )
                    {
                        dbContextTransaction.Rollback();
                    }
                    else
                    {
                        dbContextTransaction.Commit();
                    }

                    if ( entity.GetType()?.BaseType == typeof( Person ) && securityChangeDetail.ChangeDetails.Any() )
                    {
                        var workflowGuid = GlobalAttributesCache.Get().GetValue( "ChangeManagerSecurityWorkflow" ).AsGuidOrNull();
                        this.LaunchWorkflow( workflowGuid,
                            "Change Request Security Notice",
                            new Dictionary<string, string> { { "ChangeDetail", securityChangeDetail.ToJson() } } );
                    }
                }
                catch ( Exception e )
                {
                    dbContextTransaction.Rollback();
                    errors.AddRange( e.Messages() );
                }
                finally
                {
                    if ( errors.Any() )
                    {
                        ExceptionLogService.LogException( new Exception( "Error while completing change request: " + Id.ToString(),
                            new Exception( string.Join( " *** ", errors ) ) ) );
                    }
                }
            }
        }

        private void UpdateAttribute( IEntity targetEntity, string attributeKey, string value, RockContext rockContext, List<string> errors )
        {
            //Sometimes we can get change requests on deleted items.
            if ( targetEntity == null )
            {
                return;
            }

            try
            {

                if ( !( targetEntity is IHasAttributes ) )
                {
                    return;
                }
                var ihaEntity = targetEntity as IHasAttributes;
                ihaEntity.LoadAttributes( rockContext );

                ihaEntity.SetAttributeValue( attributeKey, value );
                ihaEntity.SaveAttributeValues( rockContext );
            }
            catch ( Exception e )
            {
                errors.AddRange( e.Messages() );
            }
        }

        private void DeleteEntity( IEntity targetEntity, RockContext dbContext, List<string> errors )
        {
            try
            {
                //Sometimes we can get change requests on deleted items.
                if ( targetEntity == null )
                {
                    return;
                }

                var entityTypeCache = EntityTypeCache.Get( targetEntity.TypeId );
                var entityType = entityTypeCache.GetEntityType();
                var entityService = Reflection.GetServiceForEntityType( entityType, dbContext );
                MethodInfo deleteMethodInfo = entityService.GetType().GetMethod( "Delete" );
                object[] parametersArray = new object[] { targetEntity };
                deleteMethodInfo.Invoke( entityService, parametersArray );
                dbContext.SaveChanges();
            }
            catch ( Exception e )
            {
                errors.AddRange( e.Messages() );
            }
        }


        public static IEntity CreateNewEntity( int entityTypeId, string newValue, RockContext dbContext, List<string> errors, bool addToDatabase = true )
        {
            try
            {
                var entityTypeCache = EntityTypeCache.Get( entityTypeId );
                var entityType = entityTypeCache.GetEntityType();
                var dyn = newValue.FromJsonOrNull<Dictionary<string, object>>();
                var entity = ( ( IEntity ) Activator.CreateInstance( entityType ) );
                foreach ( var key in dyn.Keys )
                {
                    var prop = entity.GetType().GetProperty( key );
                    SetProperty( entity, prop, dyn[key].ToStringSafe(), errors );
                }

                var entityService = Reflection.GetServiceForEntityType( entityType, dbContext );
                MethodInfo addMethodInfo = entityService.GetType().GetMethod( "Add" );
                object[] parametersArray = new object[] { entity };

                if ( !addToDatabase )
                {
                    return entity;
                }

                entity.Id = 0;
                addMethodInfo.Invoke( entityService, parametersArray );
                dbContext.SaveChanges();

                return entity;
            }
            catch ( Exception e )
            {
                errors.AddRange( e.Messages() );
                return null;
            }
        }

        public static void SetProperty( IEntity entity, PropertyInfo prop, string newValue, List<string> errors )
        {
            try
            {
                if ( entity == null || prop == null )
                {
                    return;
                }

                MethodInfo setter = prop.GetSetMethod();
                if ( setter == null )
                {
                    return;
                }

                if ( prop.PropertyType == typeof( string ) )
                {
                    setter.Invoke( entity, new object[] { newValue } );
                }
                else if ( prop.PropertyType == typeof( int? ) )
                {
                    setter.Invoke( entity, new object[] { newValue.AsIntegerOrNull() } );
                }
                else if ( ( prop.PropertyType == typeof( int ) ) )
                {
                    setter.Invoke( entity, new object[] { newValue.AsInteger() } );
                }
                else if ( prop.PropertyType == typeof( DateTime? ) || prop.PropertyType == typeof( DateTime ) )
                {
                    setter.Invoke( entity, new object[] { newValue.AsDateTime() } );
                }
                else if ( prop.PropertyType.IsEnum )
                {
                    setter.Invoke( entity, new object[] { newValue.AsInteger() } );
                }
                else if ( prop.PropertyType == typeof( bool ) )
                {
                    setter.Invoke( entity, new object[] { newValue.AsBoolean() } );
                }
            }
            catch ( Exception e )
            {
                errors.AddRange( e.Messages() );
            }
        }

        public static IEntity GetEntity( int entityTypeId, int entityId, RockContext dbContext, List<string> errors )
        {
            try
            {
                var entityTypeCache = EntityTypeCache.Get( entityTypeId );
                var entityType = entityTypeCache.GetEntityType();
                var entityService = Reflection.GetServiceForEntityType( entityType, dbContext );
                MethodInfo queryableMethodInfo = entityService.GetType().GetMethod( "Queryable", new Type[] { } );
                IQueryable<IEntity> entityQuery = queryableMethodInfo.Invoke( entityService, null ) as IQueryable<IEntity>;
                var entity = entityQuery.Where( x => x.Id == entityId ).FirstOrDefault();

                if ( entity != null && entity.TypeName == "Rock.Model.PersonAlias" )
                {
                    //The entity is person alias switch to person
                    entity = ( ( PersonAlias ) entity ).Person;
                }

                return entity;
            }
            catch ( Exception e )
            {
                errors.AddRange( e.Messages() );
                return null;
            }
        }
        #endregion


    }

    public partial class ChangeRequestConfiguration : EntityTypeConfiguration<ChangeRequest>
    {
        public ChangeRequestConfiguration()
        {
            this.HasRequired<EntityType>( s => s.EntityType ).WithMany().WillCascadeOnDelete( false );
            this.HasEntitySetName( "ChangeRequest" );
        }
    }
}
