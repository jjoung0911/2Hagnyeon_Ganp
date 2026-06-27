using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JWLib.DISystems
{
    [DefaultExecutionOrder(-10)] // 가장 먼저 실행될 수 있게 한다
    public class DependencyInjector : MonoBehaviour
    {
        private const BindingFlags _bindingFlags
            = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly Dictionary<Type, object> _registry = new();

        private void Awake()
        {
            IEnumerable<IDependencyProvider> providers = FindMonoBehaviours().OfType<IDependencyProvider>();
            foreach (IDependencyProvider provider in providers)
            {
                RegisterProvider(provider);
            }

            IEnumerable<MonoBehaviour> injectables = FindMonoBehaviours().Where(IsInjectable);
            foreach (MonoBehaviour injectable in injectables)
            {
                Inject(injectable);
            }
        }

        private void Inject(MonoBehaviour injectable)
        {
            // InjectAttribute가 붙은 필드들을 찾아서 레지스트리에서 해당 타입의 인스턴스를 찾아서 주입
            Type type = injectable.GetType();
            IEnumerable<FieldInfo> fields = type.GetFields(_bindingFlags)
                .Where(f => Attribute.IsDefined(f, typeof(InjectAttribute)));
            
            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                object injectInstance = Resolve(fieldType);
                Debug.Assert(injectInstance != null, $"[DependencyInjector] {type.Name}의 {field.Name} 필드에 주입할 인스턴스를 레지스트리에서 찾을 수 없습니다.");
                
                field.SetValue(injectable, injectInstance);
            }
            
            IEnumerable<MethodInfo> methods = type.GetMethods(_bindingFlags)
                .Where(method => Attribute.IsDefined(method, typeof(InjectAttribute)));
            
            foreach (MethodInfo method in methods)
            {
                Type[] requireParamTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                object[] injectInstances = requireParamTypes.Select(Resolve).ToArray();
                method.Invoke(injectable, injectInstances);
            }
        }

        private bool IsInjectable(MonoBehaviour mono)
        {
            MemberInfo[] members = mono.GetType().GetMembers(_bindingFlags);
            return members.Any(m => Attribute.IsDefined(m, typeof(InjectAttribute)));
            // InjectAttribute가 붙은 멤버가 하나라도 있으면 true 반환
        }

        private object Resolve(Type type)
        {
            // 타입에 해당하는 인스턴스를 레지스트리에서 찾아서 반환
            _registry.TryGetValue(type, out object instance);
            return instance;
        }
        private void RegisterProvider(IDependencyProvider provider)
        {
            if (Attribute.IsDefined(provider.GetType(), typeof(ProvideAttribute)))
            {
                _registry.Add(provider.GetType(), provider);
                // 레지스트리에 제공자 자신의 타입과 인스턴스를 등록
                return;
            }
            
            MethodInfo[] methods = provider.GetType().GetMethods(_bindingFlags);
            foreach (MethodInfo method in methods)
            {
                if(!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;
                Type type = method.ReturnType;
                object providerInstance = method.Invoke(provider, null);
                Debug.Assert(providerInstance != null, $"[DependencyInjector] {method.Name}에서 반환된 인스턴스가 null입니다.");
                
                
                _registry.Add(type, providerInstance);
            }
        }

        private MonoBehaviour[] FindMonoBehaviours()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        }
    }
}