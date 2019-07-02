using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using org.secc.ChangeManager.Data;
using Rock;
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

        public virtual PersonAlias RequestorAlias { get; set; }

        [Index]
        [DataMember]
        public int ApproverAliasId { get; set; }

        public virtual PersonAlias ApproverAlias { get; set; }

        [Index]
        [DataMember]
        public bool IsComplete { get; set; }

        [DataMember]
        public string RequestorComment { get; set; }

        [DataMember]
        public string ApproverComment { get; set; }

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

            this.Name = mergeObjectEntity.ToString();
        }

        #endregion

        #region Methods

        public void CompleteChanges( RockContext rockContext = null )
        {
            if ( rockContext == null )
            {
                rockContext = new RockContext();
            }
            using ( var dbContextTransaction = rockContext.Database.BeginTransaction() )
            {
                try
                {
                    IEntity entity = GetEntity( EntityTypeId, EntityId, rockContext );

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
                                targetEntity = GetEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.RelatedEntityId.Value, rockContext );
                            }
                            else
                            {
                                //new entity
                                targetEntity = CreateNewEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.NewValue, rockContext );
                                changeRecord.RelatedEntityId = targetEntity.Id;
                            }
                        }

                        //Remove records marked as delete
                        if ( changeRecord.Action == ChangeRecordAction.Delete )
                        {
                            DeleteEntity( targetEntity, rockContext );
                        }

                        else if ( changeRecord.Property.IsNotNullOrWhiteSpace() )
                        {
                            PropertyInfo prop = targetEntity.GetType().GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );

                            if ( prop.PropertyType.GetInterfaces().Any( i => i.IsInterface && i.GetInterfaces().Contains( typeof( IEntity ) ) ) )
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
                                SetProperty( targetEntity, prop, changeRecord.NewValue );
                            }
                        }
                        changeRecord.WasApplied = true;
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
                                targetEntity = GetEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.RelatedEntityId.Value, rockContext );
                            }
                            else
                            {
                                //This was a created entity that we must now murder in cold blood.
                                targetEntity = GetEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.RelatedEntityId.Value, rockContext );
                                DeleteEntity( targetEntity, rockContext );
                                changeRecord.WasApplied = false;
                                continue;
                            }
                        }

                        //Undelete
                        if ( changeRecord.RelatedEntityTypeId.HasValue && changeRecord.Action == ChangeRecordAction.Delete )
                        {
                            targetEntity = CreateNewEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.OldValue, rockContext );
                            changeRecord.RelatedEntityId = targetEntity.Id;
                        }
                        //Property changes
                        else if ( changeRecord.Property.IsNotNullOrWhiteSpace() )
                        {
                            PropertyInfo prop = targetEntity.GetType().GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );

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
                                SetProperty( targetEntity, prop, changeRecord.OldValue );
                            }
                        }
                        changeRecord.WasApplied = false;
                    }


                    rockContext.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch ( Exception e )
                {
                    dbContextTransaction.Rollback();
                    throw new Exception( "Exception occured durring saving changes.", e );
                }
            }
        }

        private void DeleteEntity( IEntity targetEntity, RockContext dbContext )
        {
            var entityTypeCache = EntityTypeCache.Get( targetEntity.TypeId );
            var entityType = entityTypeCache.GetEntityType();
            var entityService = Reflection.GetServiceForEntityType( entityType, dbContext );
            MethodInfo deleteMethodInfo = entityService.GetType().GetMethod( "Delete" );
            object[] parametersArray = new object[] { targetEntity };
            deleteMethodInfo.Invoke( entityService, parametersArray );
            dbContext.SaveChanges();
        }

        public static IEntity CreateNewEntity( int entityTypeId, string newValue, RockContext dbContext, bool addToDatabase = true )
        {
            var entityTypeCache = EntityTypeCache.Get( entityTypeId );
            var entityType = entityTypeCache.GetEntityType();
            var dyn = newValue.FromJsonOrNull<Dictionary<string, object>>();
            var entity = ( ( IEntity ) Activator.CreateInstance( entityType ) );
            foreach ( var key in dyn.Keys )
            {
                var prop = entity.GetType().GetProperty( key );
                SetProperty( entity, prop, dyn[key].ToStringSafe() );
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

        public static void SetProperty( IEntity entity, PropertyInfo prop, string newValue )
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

        public static IEntity GetEntity( int entityTypeId, int entityId, RockContext dbContext )
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
