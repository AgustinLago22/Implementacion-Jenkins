# Jenkins "Hola Mundo" CI/CD

Trabajo práctico de facultad sobre **Jenkins**: un pipeline de CI/CD mínimo pero completo, pensado para mostrar que escalarlo a un entorno real no implica un salto grande.

## Qué hace

Una API mínima en .NET responde un saludo y su versión (el número de build de Jenkins). Cada `git push` a este repo dispara un pipeline que:

1. compila y corre los tests en un contenedor efímero del SDK de .NET;
2. construye una imagen Docker etiquetada con el número de build;
3. la despliega en un contenedor **staging** local (`http://localhost:8081`);
4. verifica con un smoke test que responde.

Si un test falla, el pipeline queda en rojo y staging conserva la versión anterior.

## Arquitectura

```
GitHub ──webhook──> túnel ──> Jenkins (contenedor)
                                 │ usa el Docker del host (socket)
                                 ├─ contenedor efímero dotnet/sdk → build + test
                                 ├─ docker build → imagen hola-mundo:<build>
                                 └─ docker run → contenedor hola-mundo-staging (puerto 8081)
```

## Estructura del repo

| Ruta | Contenido |
|---|---|
| `docs/bitacora.md` | Decisiones y problemas, paso a paso |
| `docs/capturas/` | Vacía: las capturas del informe están en un documento compartido del grupo |

La estructura crece a medida que avanza el proyecto (app, `Jenkinsfile`, `infra/`).

## Requisitos

- Windows con Docker Desktop (backend WSL2)
- Git
