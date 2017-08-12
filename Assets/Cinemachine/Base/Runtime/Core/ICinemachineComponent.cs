namespace Cinemachine
{
    /// <summary>
    /// An abstract representation of a mutator acting on a Cinemachine Virtual Camera
    /// </summary>
    public interface ICinemachineComponent
    {
        /// <summary>
        /// Returns true if this object is enabled and set up to produce results.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Returns the Cinemachine Virtual Camera object that this component is attached to.
        /// </summary>
        ICinemachineCamera VirtualCamera { get; }

        /// <summary>
        /// What part of the pipeline this fits into
        /// </summary>
        CinemachineCore.Stage Stage { get; }

        /// <summary>
        /// Mutates the camera state.  This state will later be applied to the camera.
        /// </summary>
        /// <param name="curState">Input state that must be mutated</param>
        /// <param name="statePrevFrame">The state on the previous frame, deltaTime ago</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>The new CameraState object.  This is normally a modified version of curState.</returns>
        CameraState MutateCameraState(
            CameraState curState, CameraState statePrevFrame, float deltaTime);
    }
}
