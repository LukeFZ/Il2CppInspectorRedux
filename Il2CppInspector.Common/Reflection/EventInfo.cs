/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2CppInspector.IL2CPP;
using Il2CppInspector.Next.Metadata;

namespace Il2CppInspector.Reflection
{
    public class EventInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppEventDefinition Definition { get; }
        public int Index { get; }
        // Root definition: the event with Definition != null
        protected readonly EventInfo rootDefinition;

        // Information/flags about the event
        public EventAttributes Attributes { get; }

        // Custom attributes for this member
        public override IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(rootDefinition);

        // Methods for the event
        public MethodInfo AddMethod { get; }
        public MethodInfo RemoveMethod { get; }
        public MethodInfo RaiseMethod { get; }

        // Event handler delegate type
        private readonly TypeRef eventTypeReference;
        public TypeInfo EventHandlerType => eventTypeReference.Value;

        // True if the event has a special name
        public bool IsSpecialName => (Attributes & EventAttributes.SpecialName) == EventAttributes.SpecialName;

        public override MemberTypes MemberType => MemberTypes.Event;

        public EventInfo(Il2CppInspector pkg, int eventIndex, TypeInfo declaringType) :
            base(declaringType) {
            Definition = pkg.Events[eventIndex];
            MetadataToken = pkg.GetEntityToken(Definition.Token, eventIndex,
                declaringType.Assembly.ImageDefinition.EventIndex, ComputedMetadataTokenType.Event);
            Index = eventIndex;
            Name = pkg.Strings[Definition.NameIndex];
            rootDefinition = this;

            eventTypeReference = TypeRef.FromReferenceIndex(Assembly.Model, Definition.TypeIndex);
            var eventType = pkg.TypeReferences[Definition.TypeIndex];

            // Copy attributes
            Attributes = (EventAttributes) eventType.Attrs;

            // NOTE: This relies on methods being added to TypeInfo.DeclaredMethods in the same order they are defined in the Il2Cpp metadata
            // add, remove and raise are method indices from the first method of the declaring type
            if (Definition.Add >= 0)
                AddMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.MethodIndex + Definition.Add);
            if (Definition.Remove >= 0)
                RemoveMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.MethodIndex + Definition.Remove);
            if (Definition.Raise >= 0)
                RaiseMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.MethodIndex + Definition.Raise);
        }

        public EventInfo(EventInfo eventDef, TypeInfo declaringType) : base(declaringType) {
            rootDefinition = eventDef;

            Name = eventDef.Name;
            Attributes = eventDef.Attributes;
            eventTypeReference = TypeRef.FromTypeInfo(eventDef.EventHandlerType.SubstituteGenericArguments(declaringType.GetGenericArguments()));

            AddMethod = declaringType.GetMethodByDefinition(eventDef.AddMethod);
            RemoveMethod = declaringType.GetMethodByDefinition(eventDef.RemoveMethod);
            RaiseMethod = declaringType.GetMethodByDefinition(eventDef.RaiseMethod);
        }
    }
}