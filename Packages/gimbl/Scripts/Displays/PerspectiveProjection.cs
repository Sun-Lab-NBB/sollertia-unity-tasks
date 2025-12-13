/// <summary>
/// Provides the PerspectiveProjection class for off-axis projection rendering.
///
/// Calculates custom projection matrices for VR displays based on physical screen
/// position relative to the camera, enabling accurate perspective for multi-monitor setups.
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Handles off-axis perspective projection for VR displays.
/// </summary>
[ExecuteInEditMode]
public class PerspectiveProjection : MonoBehaviour
{
    /// <summary>The GameObject representing the physical projection screen.</summary>
    public GameObject projectionScreen;

    /// <summary>The display object this projection belongs to.</summary>
    public Gimbl.DisplayObject dispObj;

    /// <summary>Determines whether to estimate view frustum for culling.</summary>
    public bool estimateViewFrustrum = true;

    /// <summary>Determines whether to automatically set the near clip plane.</summary>
    public bool setNearClipPlane = true;

    /// <summary>The offset applied to the near clip plane distance.</summary>
    public float nearClipDistanceOffset = -0.01f;

    /// <summary>Enables debug logging when true.</summary>
    public bool isDebug = false;

    /// <summary>The mesh type of the projection screen (Plane or Quad).</summary>
    private string meshType;

    /// <summary>The camera component for this projection.</summary>
    private Camera cameraComponent;

    /// <summary>The projection matrix.</summary>
    private Matrix4x4 projectionMatrix;

    /// <summary>The rotation matrix.</summary>
    private Matrix4x4 rotationMatrix;

    /// <summary>The translation matrix.</summary>
    private Matrix4x4 translationMatrix;

    /// <summary>The quaternion for camera rotation.</summary>
    private Quaternion cameraRotation;

    /// <summary>The material for brightness adjustment post-processing.</summary>
    public Material material;

    /// <summary>Initializes the brightness shader material and display object reference.</summary>
    void Awake()
    {
        if (material == null)
        {
            material = new Material(Shader.Find("Hidden/BrightnessShader"));
        }
        dispObj = GetComponentInParent<Gimbl.DisplayObject>();
    }

    /// <summary>Updates the projection view after all other updates.</summary>
    void LateUpdate()
    {
        UpdateView();
    }

    /// <summary>Calculates and applies the off-axis projection matrix.</summary>
    public void UpdateView()
    {
        if (meshType == null)
        {
            meshType = projectionScreen.GetComponent<MeshFilter>().sharedMesh.name;
        }
        if (cameraComponent == null)
        {
            cameraComponent = gameObject.GetComponent<Camera>();
        }

        if (projectionScreen != null && cameraComponent != null)
        {
            Vector3 screenLowerLeft = new Vector3();
            Vector3 screenLowerRight = new Vector3();
            Vector3 screenUpperLeft = new Vector3();
            switch (meshType)
            {
                case "Plane":
                    screenLowerLeft = projectionScreen.transform.TransformPoint(new Vector3(-5.0f, 0.0f, -5.0f));
                    screenLowerRight = projectionScreen.transform.TransformPoint(new Vector3(5.0f, 0.0f, -5.0f));
                    screenUpperLeft = projectionScreen.transform.TransformPoint(new Vector3(-5.0f, 0.0f, 5.0f));
                    break;
                case "Quad":
                    screenLowerLeft = projectionScreen.transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0.0f));
                    screenLowerRight = projectionScreen.transform.TransformPoint(new Vector3(0.5f, -0.5f, 0.0f));
                    screenUpperLeft = projectionScreen.transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0.0f));
                    break;
            }

            Vector3 eyePosition = transform.position;
            float nearClipDistance = cameraComponent.nearClipPlane;
            float farClipDistance = cameraComponent.farClipPlane;

            Vector3 screenRightAxis = screenLowerRight - screenLowerLeft;
            Vector3 screenUpAxis = screenUpperLeft - screenLowerLeft;
            Vector3 eyeToLowerLeft = screenLowerLeft - eyePosition;
            Vector3 eyeToLowerRight = screenLowerRight - eyePosition;
            Vector3 eyeToUpperLeft = screenUpperLeft - eyePosition;

            if (Vector3.Dot(-Vector3.Cross(eyeToLowerLeft, eyeToUpperLeft), eyeToLowerRight) < 0.0)
            {
                if (isDebug)
                {
                    Debug.Log("Facing backface of plane");
                }
                screenUpAxis = -screenUpAxis;
                screenLowerLeft = screenUpperLeft;
                screenLowerRight = screenLowerLeft + screenRightAxis;
                screenUpperLeft = screenLowerLeft + screenUpAxis;
                eyeToLowerLeft = screenLowerLeft - eyePosition;
                eyeToLowerRight = screenLowerRight - eyePosition;
                eyeToUpperLeft = screenUpperLeft - eyePosition;
            }
            else
            {
                if (isDebug)
                {
                    Debug.Log("Not Facing backface of plane");
                }
            }

            screenRightAxis.Normalize();
            screenUpAxis.Normalize();
            Vector3 screenNormal = -Vector3.Cross(screenRightAxis, screenUpAxis);

            float eyeToScreenDistance = -Vector3.Dot(eyeToLowerLeft, screenNormal);
            if (setNearClipPlane)
            {
                nearClipDistance = eyeToScreenDistance + nearClipDistanceOffset;
                cameraComponent.nearClipPlane = nearClipDistance;
            }
            float leftEdgeDistance =
                Vector3.Dot(screenRightAxis, eyeToLowerLeft) * nearClipDistance / eyeToScreenDistance;
            float rightEdgeDistance =
                Vector3.Dot(screenRightAxis, eyeToLowerRight) * nearClipDistance / eyeToScreenDistance;
            float bottomEdgeDistance =
                Vector3.Dot(screenUpAxis, eyeToLowerLeft) * nearClipDistance / eyeToScreenDistance;
            float topEdgeDistance = Vector3.Dot(screenUpAxis, eyeToUpperLeft) * nearClipDistance / eyeToScreenDistance;

            projectionMatrix[0, 0] = 2.0f * nearClipDistance / (rightEdgeDistance - leftEdgeDistance);
            projectionMatrix[0, 1] = 0.0f;
            projectionMatrix[0, 2] = (rightEdgeDistance + leftEdgeDistance) / (rightEdgeDistance - leftEdgeDistance);
            projectionMatrix[0, 3] = 0.0f;

            projectionMatrix[1, 0] = 0.0f;
            projectionMatrix[1, 1] = 2.0f * nearClipDistance / (topEdgeDistance - bottomEdgeDistance);
            projectionMatrix[1, 2] = (topEdgeDistance + bottomEdgeDistance) / (topEdgeDistance - bottomEdgeDistance);
            projectionMatrix[1, 3] = 0.0f;

            projectionMatrix[2, 0] = 0.0f;
            projectionMatrix[2, 1] = 0.0f;
            projectionMatrix[2, 2] = (farClipDistance + nearClipDistance) / (nearClipDistance - farClipDistance);
            projectionMatrix[2, 3] = 2.0f * farClipDistance * nearClipDistance / (nearClipDistance - farClipDistance);

            projectionMatrix[3, 0] = 0.0f;
            projectionMatrix[3, 1] = 0.0f;
            projectionMatrix[3, 2] = -1.0f;
            projectionMatrix[3, 3] = 0.0f;

            rotationMatrix[0, 0] = screenRightAxis.x;
            rotationMatrix[0, 1] = screenRightAxis.y;
            rotationMatrix[0, 2] = screenRightAxis.z;
            rotationMatrix[0, 3] = 0.0f;

            rotationMatrix[1, 0] = screenUpAxis.x;
            rotationMatrix[1, 1] = screenUpAxis.y;
            rotationMatrix[1, 2] = screenUpAxis.z;
            rotationMatrix[1, 3] = 0.0f;

            rotationMatrix[2, 0] = screenNormal.x;
            rotationMatrix[2, 1] = screenNormal.y;
            rotationMatrix[2, 2] = screenNormal.z;
            rotationMatrix[2, 3] = 0.0f;

            rotationMatrix[3, 0] = 0.0f;
            rotationMatrix[3, 1] = 0.0f;
            rotationMatrix[3, 2] = 0.0f;
            rotationMatrix[3, 3] = 1.0f;

            translationMatrix[0, 0] = 1.0f;
            translationMatrix[0, 1] = 0.0f;
            translationMatrix[0, 2] = 0.0f;
            translationMatrix[0, 3] = -eyePosition.x;

            translationMatrix[1, 0] = 0.0f;
            translationMatrix[1, 1] = 1.0f;
            translationMatrix[1, 2] = 0.0f;
            translationMatrix[1, 3] = -eyePosition.y;

            translationMatrix[2, 0] = 0.0f;
            translationMatrix[2, 1] = 0.0f;
            translationMatrix[2, 2] = 1.0f;
            translationMatrix[2, 3] = -eyePosition.z;

            translationMatrix[3, 0] = 0.0f;
            translationMatrix[3, 1] = 0.0f;
            translationMatrix[3, 2] = 0.0f;
            translationMatrix[3, 3] = 1.0f;

            cameraComponent.projectionMatrix = projectionMatrix;
            cameraComponent.worldToCameraMatrix = rotationMatrix * translationMatrix;

            if (estimateViewFrustrum)
            {
                cameraRotation.SetLookRotation(
                    (0.5f * (screenLowerRight + screenUpperLeft) - eyePosition),
                    screenUpAxis
                );
                cameraComponent.transform.rotation = cameraRotation;

                if (cameraComponent.aspect >= 1.0)
                {
                    cameraComponent.fieldOfView =
                        Mathf.Rad2Deg
                        * Mathf.Atan(
                            (
                                (screenLowerRight - screenLowerLeft).magnitude
                                + (screenUpperLeft - screenLowerLeft).magnitude
                            ) / eyeToLowerLeft.magnitude
                        );
                }
                else
                {
                    cameraComponent.fieldOfView =
                        Mathf.Rad2Deg
                        / cameraComponent.aspect
                        * Mathf.Atan(
                            (
                                (screenLowerRight - screenLowerLeft).magnitude
                                + (screenUpperLeft - screenLowerLeft).magnitude
                            ) / eyeToLowerLeft.magnitude
                        );
                }
            }
        }
    }

    /// <summary>Applies brightness adjustment to the rendered image.</summary>
    /// <param name="source">The source render texture.</param>
    /// <param name="destination">The destination render texture.</param>
    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (dispObj.settings.isActive)
        {
            material.SetFloat("_brightness", dispObj.currentBrightness);
        }
        else
        {
            material.SetFloat("_brightness", 0);
        }
        Graphics.Blit(source, destination, material);
    }
}
