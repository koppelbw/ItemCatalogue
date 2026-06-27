import type { SubmitEvent } from "react";

interface LocationFormProps{
    name: string;
    description: string;
    submitting: boolean;
    isEditing: boolean;

    onNameChange: (value: string) => void;
    onDescriptionChange: (value: string) => void;
    onSubmit: (e: SubmitEvent) => void;
    onCancel: () => void;
}

function LocationForm(
    {name, description, submitting, isEditing, 
        onNameChange, onDescriptionChange, onSubmit, onCancel}: LocationFormProps){
    return (
        <form onSubmit={onSubmit}>
            <label>
            Name:{' '}
            <input value={name} onChange={(e) => onNameChange(e.target.value)} />
            </label>{' '}
            <label>
            Description:{' '}            
            <input value={description} onChange={(e) => onDescriptionChange(e.target.value)}/>
            </label>{' '}
            <button type="submit" disabled={submitting}>
            {submitting
                ? 'Saving…'
                : !isEditing
                ? 'Add location'
                : 'Save changes'}
            </button>{' '}            
            {isEditing && (
            <button type="button" onClick={onCancel}>
                Cancel
            </button>
            )}
        </form>
    );
}

export default LocationForm;