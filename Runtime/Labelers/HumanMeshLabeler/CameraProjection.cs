using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class CameraProjection : IMessageProducer
    {
        Matrix4x4 m_ProjectionMatrix;
        Matrix4x4 m_WorldToCameraMatrix;
        int m_PixelWidth;
        int m_PixelHeight;

        public CameraProjection(Camera camera)
        {
            m_ProjectionMatrix = Clone(camera.projectionMatrix);
            m_WorldToCameraMatrix = Clone(camera.worldToCameraMatrix);
            m_PixelWidth = camera.pixelWidth;
            m_PixelHeight = camera.pixelHeight;
        }

        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddInt("pixel_width", m_PixelWidth);
            builder.AddInt("pixel_height", m_PixelHeight);
            var projectionNested = builder.AddNestedMessage("projection_matrix");
            MatrixToMessage(projectionNested, m_ProjectionMatrix);
            var worldToCameraNested = builder.AddNestedMessage("world_to_camera_matrix");
            MatrixToMessage(worldToCameraNested, m_WorldToCameraMatrix);
        }

        static Matrix4x4 Clone(Matrix4x4 matrix)
        {
            var clone = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                clone[i, j] = matrix[i, j];
            return clone;
        }

        static void MatrixToMessage(IMessageBuilder builder, Matrix4x4 matrix)
        {
            for (var row = 0; row < 4; row++)
                for (var col = 0; col < 4; col++)
                    builder.AddFloat($"m{row}{col}", matrix[row, col]);
        }
    }
}
