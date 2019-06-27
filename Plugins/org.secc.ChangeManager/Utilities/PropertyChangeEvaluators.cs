using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.ChangeManager.Model;
using Rock;
using Rock.Data;
using Rock.Web.Cache;

namespace org.secc.ChangeManager.Utilities
{
    public static class PropertyChangeEvaluators
    {
        public static ChangeRecord EvaluatePropertyChange( this ChangeRequest changeRequest, object item, string property, IEntityCache newValue, bool isRelated = false, string comment = "" )
        {
            var oldValue = item.GetPropertyValue( property );
            if ( oldValue == null && newValue == null )
            {
                return null;
            }

            if ( !( oldValue is IEntity ) || ( ( IEntity ) oldValue ).Id != newValue.Id )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToJson(),
                    NewValue = newValue.ToJson(),
                    Action = ChangeRecordAction.Update,
                    IsRejected = false,
                    WasApplied = false,
                    Property = property,
                    Comment = comment
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
                return changeRecord;
            }
            return null;
        }

        public static ChangeRecord EvaluatePropertyChange( this ChangeRequest changeRequest, object item, string property, IEntity newValue, bool isRelated = false, string comment = "" )
        {
            var oldValue = item.GetPropertyValue( property );
            if ( oldValue == null && newValue == null )
            {
                return null;
            }

            if ( !( oldValue is IEntity ) || ( ( IEntity ) oldValue ).Id != newValue.Id )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToJson(),
                    NewValue = newValue.ToJson(),
                    Action = ChangeRecordAction.Update,
                    IsRejected = false,
                    WasApplied = false,
                    Property = property,
                    Comment = comment
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
                return changeRecord;
            }
            return null;
        }


        public static ChangeRecord EvaluatePropertyChange( this ChangeRequest changeRequest, object item, string property, string newValue, bool isRelated = false, string comment = "" )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( oldValue.ToStringSafe().IsNullOrWhiteSpace() && newValue.IsNullOrWhiteSpace() )
            {
                return null;
            }

            if ( !( oldValue is string ) || ( string ) oldValue != newValue )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToStringSafe(),
                    NewValue = newValue,
                    Action = ChangeRecordAction.Update,
                    IsRejected = false,
                    WasApplied = false,
                    Property = property,
                    Comment = comment
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
                return changeRecord;
            }
            return null;
        }

        public static ChangeRecord EvaluatePropertyChange( this ChangeRequest changeRequest, object item, string property, int? newValue, bool isRelated = false, string comment = "" )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( oldValue == null && newValue == null )
            {
                return null;
            }

            if ( !( oldValue is int? ) || ( int? ) oldValue != newValue )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToStringSafe(),
                    NewValue = newValue.ToStringSafe(),
                    Action = ChangeRecordAction.Update,
                    IsRejected = false,
                    WasApplied = false,
                    Property = property,
                    Comment = comment
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
                return changeRecord;
            }
            return null;
        }

        public static ChangeRecord EvaluatePropertyChange( this ChangeRequest changeRequest, object item, string property, bool newValue, bool isRelated = false, string comment = "" )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( !( oldValue is bool ) || ( bool ) oldValue != newValue )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToStringSafe(),
                    NewValue = newValue.ToStringSafe(),
                    Action = ChangeRecordAction.Update,
                    IsRejected = false,
                    WasApplied = false,
                    Property = property,
                    Comment = comment
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
                return changeRecord;
            }
            return null;
        }

        public static ChangeRecord EvaluatePropertyChange( this ChangeRequest changeRequest, object item, string property, Enum newValue, bool isRelated = false, string comment = "" )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( oldValue == null && newValue == null )
            {
                return null;
            }

            if ( !( oldValue is Enum ) || !newValue.Equals( ( Enum ) oldValue ) )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = ( ( Enum ) oldValue ).ConvertToInt().ToString(),
                    NewValue = newValue.ConvertToInt().ToString(),
                    Action = ChangeRecordAction.Update,
                    IsRejected = false,
                    WasApplied = false,
                    Property = property,
                    Comment = comment
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
                return changeRecord;
            }
            return null;
        }

        public static ChangeRecord EvaluatePropertyChange( this ChangeRequest changeRequest, object item, string property, DateTime? newValue, bool isRelated = false, string comment = "" )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( oldValue == null && newValue == null )
            {
                return null;
            }

            if ( !( oldValue is DateTime? ) || ( DateTime? ) oldValue != newValue )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = ( ( DateTime? ) oldValue ).ToStringSafe(),
                    NewValue = newValue.ToStringSafe(),
                    Action = ChangeRecordAction.Update,
                    IsRejected = false,
                    WasApplied = false,
                    Property = property,
                    Comment = comment
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
                return changeRecord;
            }
            return null;
        }

        public static ChangeRecord AddEntity( this ChangeRequest changeRequest, IEntity entity, RockContext rockContext, bool isRelated = false, string comment = "" )
        {
            ChangeRecord changeRecord = new ChangeRecord
            {
                OldValue = "",
                NewValue = entity.ToJson(),
                IsRejected = false,
                WasApplied = false,
                Action = ChangeRecordAction.Create,
                Comment = comment
            };
            if ( isRelated )
            {
                changeRecord.RelatedEntityId = 0;
                changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
            }

            changeRequest.ChangeRecords.Add( changeRecord );
            return changeRecord;
        }

        public static ChangeRecord DeleteEntity( this ChangeRequest changeRequest, IEntity entity, bool isRelated = false, string comment = "" )
        {
            var serializedEntity = entity.ToJson();
            var changeRecord = new ChangeRecord()
            {
                OldValue = serializedEntity,
                NewValue = "",
                Action = ChangeRecordAction.Delete,
                IsRejected = false,
                WasApplied = false,
                Property = "",
                Comment = comment

            };

            if ( isRelated )
            {
                changeRecord.RelatedEntityId = entity.Id;
                changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
            }

            changeRequest.ChangeRecords.Add( changeRecord );
            return changeRecord;
        }
    }
}
